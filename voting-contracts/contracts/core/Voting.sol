// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./VoterRegistry.sol";

contract Voting {
    // ─── Types ───────────────────────────────────────────────
    struct Candidate {
        uint256 id;
        string name;
        uint256 voteCount;
    }

    // ─── State ───────────────────────────────────────────────
    address public owner;
    bool public votingOpen;
    address public zkVerifier;

    VoterRegistry public registry;

    Candidate[] public candidates;
    mapping(address => bool) public hasVoted;

    // ─── Events ──────────────────────────────────────────────
    event CandidateAdded(uint256 indexed id, string name);
    event Voted(address indexed voter, uint256 indexed candidateId);
    event VotingStarted();
    event VotingEnded();
    event OwnershipTransferred(
        address indexed previousOwner,
        address indexed newOwner
    );

    // ─── Modifiers ───────────────────────────────────────────
    modifier onlyOwner() {
        require(msg.sender == owner, "Not the owner");
        _;
    }

    modifier whenOpen() {
        require(votingOpen, "Voting is not open");
        _;
    }

    modifier notVoted() {
        require(!hasVoted[msg.sender], "Already voted");
        _;
    }

    // ─── Constructor ─────────────────────────────────────────
    constructor(address _registry, address _zkVerifier) {
        owner = msg.sender;
        registry = VoterRegistry(_registry);
        zkVerifier = _zkVerifier;
    }

    // ─── Ownership ───────────────────────────────────────────
    function transferOwnership(address _newOwner) external onlyOwner {
        require(_newOwner != address(0), "Zero address");
        emit OwnershipTransferred(owner, _newOwner);
        owner = _newOwner;
    }

    // ─── Owner functions ─────────────────────────────────────
    function addCandidate(string calldata _name) external onlyOwner {
        require(!votingOpen, "Cannot add candidates while voting is open");
        uint256 id = candidates.length;
        candidates.push(Candidate({id: id, name: _name, voteCount: 0}));
        emit CandidateAdded(id, _name);
    }

    function startVoting() external onlyOwner {
        require(candidates.length > 0, "No candidates added");
        require(!votingOpen, "Voting already open");
        votingOpen = true;
        emit VotingStarted();
    }

    function endVoting() external onlyOwner whenOpen {
        votingOpen = false;
        emit VotingEnded();
    }

    // ─── Standard vote ───────────────────────────────────────
    function vote(
        address voter,
        uint256 _candidateId
    ) external whenOpen notVoted {
        require(
            registry.isEligible(address(this), voter),
            "Not eligible to vote"
        );

        require(!hasVoted[voter], "Already voted");

        require(_candidateId < candidates.length, "Invalid candidate");

        hasVoted[voter] = true;

        candidates[_candidateId].voteCount += 1;

        emit Voted(voter, _candidateId);
    }

    // ─── View functions ──────────────────────────────────────
    function getCandidateCount() external view returns (uint256) {
        return candidates.length;
    }

    function getCandidate(
        uint256 _id
    ) external view returns (string memory name, uint256 voteCount) {
        require(_id < candidates.length, "Invalid candidate");
        Candidate storage c = candidates[_id];
        return (c.name, c.voteCount);
    }

    function getWinner()
        external
        view
        returns (
            uint256[] memory winnerIds,
            string[] memory winnerNames,
            uint256 topVotes
        )
    {
        require(candidates.length > 0, "No candidates");

        for (uint256 i = 0; i < candidates.length; i++) {
            if (candidates[i].voteCount > topVotes) {
                topVotes = candidates[i].voteCount;
            }
        }

        uint256 count;
        for (uint256 i = 0; i < candidates.length; i++) {
            if (candidates[i].voteCount == topVotes) count++;
        }

        winnerIds = new uint256[](count);
        winnerNames = new string[](count);
        uint256 idx;
        for (uint256 i = 0; i < candidates.length; i++) {
            if (candidates[i].voteCount == topVotes) {
                winnerIds[idx] = candidates[i].id;
                winnerNames[idx] = candidates[i].name;
                idx++;
            }
        }
    }
}
