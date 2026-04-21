// SPDX-License-Identifier: MIT
// Required by Solidity — declares the open source license.
pragma solidity ^0.8.20;

contract VotingContract {
    // ================================================================
    // STRUCTS — data blueprints
    // ================================================================

    // Represents one candidate in an election
    struct Candidate {
        uint id; // 1, 2, 3... (matches OnChainIndex in your SQL Candidates table)
        string name; // "Alice Khan"
        string party; // "Independent" or party name
        uint voteCount; // increments by 1 every time someone votes for this candidate
    }

    // Represents one election
    struct Election {
        uint id;
        string title;
        uint startTime; // Unix timestamp — seconds since Jan 1 1970
        uint endTime; // Unix timestamp
        bool exists; // used to check if an electionId is valid
        uint candidateCount; // how many candidates are in this election
    }

    // ================================================================
    // STATE VARIABLES — permanently stored on the blockchain
    // ================================================================

    // The Ethereum address that deployed this contract.
    // Only this address can call admin functions (createElection, registerVoter).
    // In your system this will be your .NET API's wallet address.
    address public owner;

    // Total number of elections ever created.
    // Also used as the ID for the next election (auto-increment).
    uint public electionCount;

    // electionId => Election struct
    // Example: elections[1] gives you the first election's data
    mapping(uint => Election) public elections;

    // electionId => candidateId => Candidate struct
    // Example: candidates[1][2] gives you candidate #2 in election #1
    mapping(uint => mapping(uint => Candidate)) public candidates;

    // electionId => voterAddress => true/false
    // Tracks which wallets are whitelisted for each election.
    // Set to true by registerVoter(). Checked before allowing a vote.
    mapping(uint => mapping(address => bool)) public registeredVoters;

    // electionId => voterAddress => true/false
    // Tracks which wallets have already cast their vote.
    // This is what PREVENTS double voting — once true, castVote() rejects them.
    mapping(uint => mapping(address => bool)) public hasVoted;

    // ================================================================
    // EVENTS — blockchain logs that your .NET API listens to
    // ================================================================

    // Emitted when admin creates a new election
    event ElectionCreated(uint indexed electionId, string title);

    // Emitted when a voter is whitelisted for an election
    event VoterRegistered(uint indexed electionId, address indexed voter);

    // Emitted when a vote is successfully cast
    // Your SignalR hub listens for this to push live updates to Angular
    event VoteCast(
        uint indexed electionId,
        uint indexed candidateId,
        address indexed voter
    );

    // Emitted when an election's time window closes
    event ElectionClosed(uint indexed electionId);

    // ================================================================
    // MODIFIERS — reusable condition guards
    // ================================================================

    // Blocks anyone who is not the contract owner (your .NET API wallet)
    modifier onlyOwner() {
        require(msg.sender == owner, "Not authorized: caller is not the owner");
        _; // the _ means "now run the actual function body"
    }

    // Blocks calls that reference a non-existent electionId
    modifier electionExists(uint _electionId) {
        require(elections[_electionId].exists, "Election does not exist");
        _;
    }

    // Blocks votes outside the election's time window
    modifier electionActive(uint _electionId) {
        require(
            block.timestamp >= elections[_electionId].startTime,
            "Election has not started yet"
        );
        require(
            block.timestamp <= elections[_electionId].endTime,
            "Election has already ended"
        );
        _;
    }

    // ================================================================
    // CONSTRUCTOR — runs ONCE when the contract is deployed
    // ================================================================

    // Sets the deployer's address as owner.
    // Your deploy.js script runs this — so the .NET API wallet becomes owner.
    constructor() {
        owner = msg.sender;
    }

    // ================================================================
    // ADMIN FUNCTIONS — only callable by owner (.NET API)
    // ================================================================

    /**
     * Creates a new election and its candidates in one transaction.
     *
     * Called by: ElectionsController.cs (via BlockchainService.cs)
     * When: Admin clicks "Activate Election" in Angular
     *
     * @param _title             Election name e.g. "Student Council 2026"
     * @param _startTime         Unix timestamp for voting start
     * @param _endTime           Unix timestamp for voting end
     * @param _candidateNames    Array of candidate names ["Alice", "Bob"]
     * @param _candidateParties  Array of party names ["Party A", "Party B"]
     */
    function createElection(
        string memory _title,
        uint _startTime,
        uint _endTime,
        string[] memory _candidateNames,
        string[] memory _candidateParties
    ) external onlyOwner {
        // Validate inputs before writing anything
        require(_endTime > _startTime, "End time must be after start time");
        require(_endTime > block.timestamp, "End time must be in the future");
        require(
            _candidateNames.length == _candidateParties.length,
            "Candidate names and parties arrays must match in length"
        );
        require(
            _candidateNames.length >= 2,
            "Election needs at least 2 candidates"
        );

        // Increment counter and use it as the new election's ID
        electionCount++;
        uint electionId = electionCount;

        // Write the Election struct to blockchain storage
        elections[electionId] = Election({
            id: electionId,
            title: _title,
            startTime: _startTime,
            endTime: _endTime,
            exists: true,
            candidateCount: _candidateNames.length
        });

        // Write each Candidate struct — candidateId starts at 1 (not 0)
        for (uint i = 0; i < _candidateNames.length; i++) {
            candidates[electionId][i + 1] = Candidate({
                id: i + 1,
                name: _candidateNames[i],
                party: _candidateParties[i],
                voteCount: 0
            });
        }

        emit ElectionCreated(electionId, _title);
    }

    /**
     * Whitelists a single voter for a specific election.
     *
     * Called by: VotersController.cs (via BlockchainService.cs)
     * When: Admin uploads CSV or manually registers a voter
     *
     * @param _electionId  Which election to register for
     * @param _voter       Ethereum wallet address of the voter
     */
    function registerVoter(
        uint _electionId,
        address _voter
    ) external onlyOwner electionExists(_electionId) {
        require(
            !registeredVoters[_electionId][_voter],
            "Voter is already registered for this election"
        );

        registeredVoters[_electionId][_voter] = true;

        emit VoterRegistered(_electionId, _voter);
    }

    // ================================================================
    // VOTER FUNCTION — the core of the entire system
    // ================================================================

    /**
     * Casts a vote. This is the most important function.
     * msg.sender IS the voter — the contract reads their address directly.
     * There is no way to fake this or vote on behalf of someone else.
     *
     * Called by: VoteController.cs (via BlockchainService.cs)
     * When: Voter clicks "Confirm Vote" in Angular
     *
     * @param _electionId   Which election to vote in
     * @param _candidateId  Which candidate to vote for (1-based index)
     */
    function castVote(
        uint _electionId,
        uint _candidateId
    ) external electionExists(_electionId) electionActive(_electionId) {
        // Check 1: is this wallet on the whitelist?
        require(
            registeredVoters[_electionId][msg.sender],
            "You are not registered to vote in this election"
        );

        // Check 2: has this wallet already voted? (DOUBLE VOTE PREVENTION)
        require(
            !hasVoted[_electionId][msg.sender],
            "You have already voted in this election"
        );

        // Check 3: is the candidate ID valid?
        require(
            _candidateId >= 1 &&
                _candidateId <= elections[_electionId].candidateCount,
            "Invalid candidate selected"
        );

        // Mark this voter as having voted — BEFORE incrementing count
        // (prevents re-entrancy attacks)
        hasVoted[_electionId][msg.sender] = true;

        // Increment the candidate's vote count — permanently on blockchain
        candidates[_electionId][_candidateId].voteCount++;

        // Emit event — Nethereum in your .NET API catches this
        // and triggers SignalR to push updated counts to Angular
        emit VoteCast(_electionId, _candidateId, msg.sender);
    }

    // ================================================================
    // VIEW FUNCTIONS — free reads, no gas, called constantly
    // ================================================================

    /**
     * Check if a wallet is registered for an election.
     * Called by VoteService.cs before allowing vote submission.
     */
    function isVoterRegistered(
        uint _electionId,
        address _voter
    ) external view returns (bool) {
        return registeredVoters[_electionId][_voter];
    }

    /**
     * Check if a wallet has already voted.
     * Called by Angular to show "You have already voted" message.
     */
    function hasVoterVoted(
        uint _electionId,
        address _voter
    ) external view returns (bool) {
        return hasVoted[_electionId][_voter];
    }

    /**
     * Get all results for an election in one call.
     * Called by ResultsService.cs to build the results dashboard.
     * Returns three parallel arrays: ids, names, vote counts.
     */
    function getResults(
        uint _electionId
    )
        external
        view
        electionExists(_electionId)
        returns (uint[] memory ids, string[] memory names, uint[] memory votes)
    {
        uint count = elections[_electionId].candidateCount;

        ids = new uint[](count);
        names = new string[](count);
        votes = new uint[](count);

        for (uint i = 0; i < count; i++) {
            Candidate memory c = candidates[_electionId][i + 1];
            ids[i] = c.id;
            names[i] = c.name;
            votes[i] = c.voteCount;
        }

        // Solidity returns multiple values — Nethereum unpacks these
        // into a C# object automatically
    }

    /**
     * Get basic info about an election.
     * Called by Angular to display election details page.
     */
    function getElection(
        uint _electionId
    )
        external
        view
        electionExists(_electionId)
        returns (
            string memory title,
            uint startTime,
            uint endTime,
            uint candidateCount
        )
    {
        Election memory e = elections[_electionId];
        return (e.title, e.startTime, e.endTime, e.candidateCount);
    }

    /**
     * Get one candidate's details including their current vote count.
     * Called by ResultsService.cs per candidate if needed individually.
     */
    function getCandidate(
        uint _electionId,
        uint _candidateId
    )
        external
        view
        electionExists(_electionId)
        returns (
            uint id,
            string memory name,
            string memory party,
            uint voteCount
        )
    {
        Candidate memory c = candidates[_electionId][_candidateId];
        return (c.id, c.name, c.party, c.voteCount);
    }
}
