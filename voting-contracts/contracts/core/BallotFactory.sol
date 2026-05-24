// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./Voting.sol";

contract BallotFactory {
    struct BallotInfo {
        uint256 ballotId;
        address contractAddress;
        address owner;
        string title;
    }

    mapping(uint256 => BallotInfo) private ballots;

    function getBallotInfo(
        uint256 _ballotId
    ) external view returns (BallotInfo memory) {
        return ballots[_ballotId];
    }

    uint256 public ballotCount;

    address public registryAddress;
    address public zkVerifierAddress;

    event BallotCreated(
        uint256 indexed ballotId,
        address indexed contractAddress,
        address indexed owner,
        string title
    );

    constructor(address _registryAddress, address _zkVerifierAddress) {
        registryAddress = _registryAddress;
        zkVerifierAddress = _zkVerifierAddress;
    }

    function createBallot(
        string memory _title,
        string[] memory _candidateNames
    ) external returns (uint256) {
        Voting newVoting = new Voting(registryAddress, zkVerifierAddress);

        for (uint256 i = 0; i < _candidateNames.length; i++) {
            newVoting.addCandidate(_candidateNames[i]);
        }

        newVoting.transferOwnership(msg.sender);

        ballots[ballotCount] = BallotInfo({
            ballotId: ballotCount,
            contractAddress: address(newVoting),
            owner: msg.sender,
            title: _title
        });

        emit BallotCreated(ballotCount, address(newVoting), msg.sender, _title);

        ballotCount++;

        return ballotCount - 1;
    }

    function getBallotCount() external view returns (uint256) {
        return ballotCount;
    }

    function getVotingContract(
        uint256 _ballotId
    ) external view returns (Voting) {
        return Voting(ballots[_ballotId].contractAddress);
    }

    function getAllBallots() external view returns (BallotInfo[] memory) {
        BallotInfo[] memory all = new BallotInfo[](ballotCount);

        for (uint256 i = 0; i < ballotCount; i++) {
            all[i] = ballots[i];
        }

        return all;
    }
}
