using Nethereum.Contracts;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System.Numerics;
using VotingAPI.Models.DTOs.Result;
using VotingAPI.Models.DTOs.Vote;
using VotingAPI.Services.Blockchain.Generated.AccessControl;
using VotingAPI.Services.Blockchain.Generated.BallotFactory;
using VotingAPI.Services.Blockchain.Generated.BallotFactory.ContractDefinition;
using VotingAPI.Services.Blockchain.Generated.VoterRegistry;
using VotingAPI.Services.Blockchain.Generated.Voting;
using VotingAPI.Services.Interfaces;

namespace VotingAPI.Services
{
    public class BlockchainService : IBlockchainService
    {
        private readonly Web3 web3;
        private readonly string adminWalletAddress;
        private readonly string accessControlAddress;
        private readonly string zkVerifierAddress;
        private readonly string voterRegistryAddress;
        private readonly string ballotFactoryAddress;
        private readonly string resultAggregatorAddress;

        public BlockchainService(IConfiguration config)
        {
            var privateKey = config["BlockchainSettings:AdminPrivateKey"]!;
            var account = new Account(privateKey);
            adminWalletAddress = account.Address;
            web3 = new Web3(account, config["BlockchainSettings:NodeUrl"]);
            accessControlAddress = config["BlockchainSettings:AccessControlAddress"] ?? throw new InvalidOperationException("AccessControlAddress is missing");
            zkVerifierAddress = config["BlockchainSettings:ZKVerifierAddress"] ?? throw new InvalidOperationException("ZKVerifierAddress is missing");
            voterRegistryAddress = config["BlockchainSettings:VoterRegistryAddress"] ?? throw new InvalidOperationException("VoterRegistryAddress is missing");
            ballotFactoryAddress = config["BlockchainSettings:BallotFactoryAddress"] ?? throw new ArgumentException("BallotFactoryAddress is missing");
            resultAggregatorAddress = config["BlockchainSettings:ResultAggregatorAddress"] ?? throw new ArgumentException("ResultAggregatorAddress is missing");

        }

        public async Task<string> CreateNewElectionAsync(string title, List<string> candidateNames)
        {
            var ballotFactoryService = new BallotFactoryService(web3, ballotFactoryAddress);

            var createBallotFunction = new CreateBallotFunction
            {
                Title = title,
                CandidateNames = candidateNames
            };

            // 1. Execute the transaction on the blockchain
            var receipt = await ballotFactoryService.CreateBallotRequestAndWaitForReceiptAsync(createBallotFunction);

            // 2. Decode the BallotCreated event to get the new contract address
            var ballotCreatedEvent = receipt.DecodeAllEvents<BallotCreatedEventDTO>();
            var votingContractAddress = ballotCreatedEvent.FirstOrDefault()?.Event.ContractAddress;

            if (string.IsNullOrEmpty(votingContractAddress))
                throw new InvalidOperationException("Failed to retrieve the new election contract address from the blockchain events.");

            return votingContractAddress;
        }

        public async Task StartVotingAsync(string votingContractAddress)
        {
            var votingService = new VotingService(web3, votingContractAddress);
            await votingService.StartVotingRequestAndWaitForReceiptAsync();
        }

        public async Task EndVotingAsync(string votingContractAddress)
        {
            var votingService = new VotingService(web3, votingContractAddress);
            await votingService.EndVotingRequestAndWaitForReceiptAsync();
        }

        public async Task GrantBallotAdminRoleAsync(string votingContractAddress)
        {
            var accessControlService = new AccessControlService(web3, accessControlAddress);

            var role = await accessControlService.BallotAdminQueryAsync();

            await accessControlService.GrantBallotRoleRequestAndWaitForReceiptAsync(votingContractAddress, role, adminWalletAddress);
        }

        public async Task SetEligibilityAsync(string votingContractAddress, string walletAddress)
        {
            var voterRegistryService = new VoterRegistryService(web3, voterRegistryAddress);

            await voterRegistryService.SetEligibilityRequestAndWaitForReceiptAsync(votingContractAddress, walletAddress, true);
        }

        public async Task<List<CandidateResultDTO>> GetResultsAsync(string votingContractAddress)
        {
            var votingService = new VotingService(web3, votingContractAddress);
            
            var count = await votingService.GetCandidateCountQueryAsync();

            var results = new List<CandidateResultDTO>();

            for (int i = 0; i < count; i++)
            {
                var candidate = await votingService.GetCandidateQueryAsync(i);

                results.Add(new CandidateResultDTO
                {
                    CandidateName = candidate.Name,
                    VoteCount = (long)candidate.VoteCount
                });
            }

            return results;
        }
    }
}