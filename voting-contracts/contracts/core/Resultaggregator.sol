// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./BallotFactory.sol";

contract ResultAggregator {

    // ─── Types ───────────────────────────────────────────────
    struct CandidateResult {
        uint256 candidateId;
        string  name;
        uint256 voteCount;
        uint256 percentage;
    }

    struct BallotResult {
        uint256          ballotId;
        address          contractAddress;
        address          owner;
        string           title;
        bool             votingOpen;
        uint256          totalVotes;
        CandidateResult[] candidates;
        uint256[]        winnerIds;
    }

    struct LeaderboardEntry {
        uint256 ballotId;
        address contractAddress;
        string  title;
        uint256 totalVotes;
        string  topCandidateName;
        uint256 topCandidateVotes;
    }

    // ─── State ───────────────────────────────────────────────
    BallotFactory public factory;

    // ─── Constructor ─────────────────────────────────────────
    constructor(address _factory) {
        require(_factory != address(0), "Zero address");
        factory = BallotFactory(_factory);
    }

    // ─── Core aggregation ────────────────────────────────────
    function getBallotResult(uint256 _ballotId)
        external view
        returns (BallotResult memory result)
    {
        BallotFactory.BallotInfo memory info = factory.getBallotInfo(_ballotId);
        Voting voting = Voting(info.contractAddress);

        uint256 count = voting.getCandidateCount();

        uint256 total;
        for (uint256 i = 0; i < count; i++) {
            (, uint256 v) = voting.getCandidate(i);
            total += v;
        }

        CandidateResult[] memory cands = new CandidateResult[](count);
        for (uint256 i = 0; i < count; i++) {
            (string memory name, uint256 votes) = voting.getCandidate(i);
            cands[i] = CandidateResult({
                candidateId: i,
                name:        name,
                voteCount:   votes,
                percentage:  total > 0 ? (votes * 10_000) / total : 0
            });
        }

        (uint256[] memory winnerIds, ,) = voting.getWinner();

        result.ballotId        = _ballotId;
        result.contractAddress = info.contractAddress;
        result.owner           = info.owner;
        result.title           = info.title;
        result.votingOpen      = voting.votingOpen();
        result.totalVotes      = total;
        result.candidates      = cands;
        result.winnerIds       = winnerIds;
    }

    function getAllResults(uint256 _offset, uint256 _limit)
        external view
        returns (BallotResult[] memory results, uint256 total)
    {
        total = factory.getBallotCount();
        if (_offset >= total) return (new BallotResult[](0), total);

        uint256 end = _offset + _limit;
        if (end > total) end = total;
        uint256 size = end - _offset;

        results = new BallotResult[](size);
        for (uint256 i = 0; i < size; i++) {
            results[i] = this.getBallotResult(_offset + i);
        }
    }

    // ─── Cross-ballot analytics ──────────────────────────────
    function getLeaderboard(uint256 _offset, uint256 _limit)
        external view
        returns (LeaderboardEntry[] memory board)
    {
        uint256 total = factory.getBallotCount();
        if (_offset >= total) return new LeaderboardEntry[](0);

        uint256 end = _offset + _limit;
        if (end > total) end = total;
        uint256 size = end - _offset;

        LeaderboardEntry[] memory entries = new LeaderboardEntry[](size);
        for (uint256 i = 0; i < size; i++) {
            uint256 bid = _offset + i;
            BallotFactory.BallotInfo memory info = factory.getBallotInfo(bid);
            Voting voting = Voting(info.contractAddress);

            uint256 cCount = voting.getCandidateCount();
            uint256 totalVotes;
            string memory topName;
            uint256 topVotes;

            for (uint256 j = 0; j < cCount; j++) {
                (string memory n, uint256 v) = voting.getCandidate(j);
                totalVotes += v;
                if (v > topVotes) { topVotes = v; topName = n; }
            }

            entries[i] = LeaderboardEntry({
                ballotId:          bid,
                contractAddress:   info.contractAddress,
                title:             info.title,
                totalVotes:        totalVotes,
                topCandidateName:  topName,
                topCandidateVotes: topVotes
            });
        }

        // Insertion sort descending by totalVotes
        for (uint256 i = 1; i < size; i++) {
            LeaderboardEntry memory key = entries[i];
            int256 j = int256(i) - 1;
            while (j >= 0 && entries[uint256(j)].totalVotes < key.totalVotes) {
                entries[uint256(j + 1)] = entries[uint256(j)];
                j--;
            }
            entries[uint256(j + 1)] = key;
        }

        return entries;
    }

    function getGlobalTotalVotes() external view returns (uint256 total) {
        uint256 count = factory.getBallotCount();
        for (uint256 i = 0; i < count; i++) {
            Voting voting = factory.getVotingContract(i);
            uint256 cCount = voting.getCandidateCount();
            for (uint256 j = 0; j < cCount; j++) {
                (, uint256 v) = voting.getCandidate(j);
                total += v;
            }
        }
    }

    function getVotesByCandidate(string calldata _name)
        external view
        returns (uint256 totalVotes, uint256 ballotsFound)
    {
        uint256 count = factory.getBallotCount();
        for (uint256 i = 0; i < count; i++) {
            Voting voting = factory.getVotingContract(i);
            uint256 cCount = voting.getCandidateCount();
            for (uint256 j = 0; j < cCount; j++) {
                (string memory n, uint256 v) = voting.getCandidate(j);
                if (keccak256(bytes(n)) == keccak256(bytes(_name))) {
                    totalVotes  += v;
                    ballotsFound++;
                }
            }
        }
    }

    function getActiveBallots()
        external view
        returns (BallotFactory.BallotInfo[] memory active)
    {
        BallotFactory.BallotInfo[] memory all = factory.getAllBallots();
        uint256 activeCount;

        for (uint256 i = 0; i < all.length; i++) {
            if (Voting(all[i].contractAddress).votingOpen()) activeCount++;
        }

        active = new BallotFactory.BallotInfo[](activeCount);
        uint256 idx;
        for (uint256 i = 0; i < all.length; i++) {
            if (Voting(all[i].contractAddress).votingOpen()) {
                active[idx++] = all[i];
            }
        }
    }

    function getParticipationShare()
        external view
        returns (uint256[] memory ballotIds, uint256[] memory shares)
    {
        uint256 count = factory.getBallotCount();
        uint256[] memory totals = new uint256[](count);
        uint256 grand;

        for (uint256 i = 0; i < count; i++) {
            Voting voting = factory.getVotingContract(i);
            uint256 cCount = voting.getCandidateCount();
            for (uint256 j = 0; j < cCount; j++) {
                (, uint256 v) = voting.getCandidate(j);
                totals[i] += v;
            }
            grand += totals[i];
        }

        ballotIds = new uint256[](count);
        shares    = new uint256[](count);
        for (uint256 i = 0; i < count; i++) {
            ballotIds[i] = i;
            shares[i]    = grand > 0 ? (totals[i] * 10_000) / grand : 0;
        }
    }
}