// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../security/AccessControl.sol";

contract VoterRegistry {

    // ─── Types ───────────────────────────────────────────────
    enum VoterStatus { Unregistered, Pending, Approved, Revoked }

    struct Voter {
        address     wallet;
        string      name;
        VoterStatus status;
        uint256     registeredAt;
        uint256     approvedAt;
    }

    // ─── State ───────────────────────────────────────────────
    AccessControl public acl;

    mapping(address => Voter) private voters;
    address[]                 private voterList;

    mapping(address => mapping(address => bool)) private ballotEligibility;

    // ─── Events ──────────────────────────────────────────────
    event VoterRegistered(address indexed voter, string name);
    event VoterApproved(address indexed voter, address indexed approvedBy);
    event VoterRevoked(address indexed voter, address indexed revokedBy);
    event EligibilitySet(address indexed ballot, address indexed voter, bool eligible);

    // ─── Modifiers ───────────────────────────────────────────
    modifier onlyAdmin() {
        require(
            acl.hasRole(acl.ADMIN(), msg.sender),
            "Registry: not admin"
        );
        _;
    }

    modifier onlyBallotAdmin(address ballot) {
        require(
            acl.isAuthorized(ballot, acl.BALLOT_ADMIN(), msg.sender),
            "Registry: not ballot admin"
        );
        _;
    }

    // ─── Constructor ─────────────────────────────────────────
    constructor(address _acl) {
        acl = AccessControl(_acl);
    }

    // ─── Registration ────────────────────────────────────────
    function register(string calldata _name) external {
        require(
            voters[msg.sender].status == VoterStatus.Unregistered,
            "Already registered"
        );
        require(bytes(_name).length > 0, "Name required");

        voters[msg.sender] = Voter({
            wallet:       msg.sender,
            name:         _name,
            status:       VoterStatus.Pending,
            registeredAt: block.timestamp,
            approvedAt:   0
        });

        voterList.push(msg.sender);
        emit VoterRegistered(msg.sender, _name);
    }

    // ─── Admin: approval / revocation ────────────────────────
    function approveVoter(address _voter) external onlyAdmin {
        require(voters[_voter].status == VoterStatus.Pending, "Not pending");
        voters[_voter].status     = VoterStatus.Approved;
        voters[_voter].approvedAt = block.timestamp;
        emit VoterApproved(_voter, msg.sender);
    }

    function batchApprove(address[] calldata _voters) external onlyAdmin {
        for (uint256 i = 0; i < _voters.length; i++) {
            address v = _voters[i];
            if (voters[v].status == VoterStatus.Pending) {
                voters[v].status     = VoterStatus.Approved;
                voters[v].approvedAt = block.timestamp;
                emit VoterApproved(v, msg.sender);
            }
        }
    }

    function revokeVoter(address _voter) external onlyAdmin {
        require(voters[_voter].status == VoterStatus.Approved, "Not approved");
        voters[_voter].status = VoterStatus.Revoked;
        emit VoterRevoked(_voter, msg.sender);
    }

    // ─── Per-ballot eligibility ───────────────────────────────
    function setEligibility(
        address ballot,
        address _voter,
        bool    _eligible
    ) external onlyBallotAdmin(ballot) {
        require(
            voters[_voter].status != VoterStatus.Unregistered,
            "Unknown voter"
        );
        ballotEligibility[ballot][_voter] = _eligible;
        emit EligibilitySet(ballot, _voter, _eligible);
    }

    // ─── View functions ──────────────────────────────────────
    function isApproved(address _voter) external view returns (bool) {
        return voters[_voter].status == VoterStatus.Approved;
    }

    function isEligible(address _ballot, address _voter)
        external view returns (bool)
    {
        VoterStatus s = voters[_voter].status;
        if (s == VoterStatus.Unregistered || s == VoterStatus.Revoked)
            return false;
        if (s == VoterStatus.Pending) return false;

        bool hasOverride = ballotEligibility[_ballot][_voter];
        return hasOverride || s == VoterStatus.Approved;
    }

    function getVoter(address _voter)
        external view
        returns (
            string      memory name,
            VoterStatus        status,
            uint256            registeredAt,
            uint256            approvedAt
        )
    {
        Voter storage v = voters[_voter];
        return (v.name, v.status, v.registeredAt, v.approvedAt);
    }

    function getVoterCount() external view returns (uint256) {
        return voterList.length;
    }

    function getVotersPaginated(uint256 _offset, uint256 _limit)
        external view
        returns (Voter[] memory page)
    {
        uint256 total = voterList.length;
        if (_offset >= total) return new Voter[](0);

        uint256 end = _offset + _limit;
        if (end > total) end = total;

        page = new Voter[](end - _offset);
        for (uint256 i = _offset; i < end; i++) {
            page[i - _offset] = voters[voterList[i]];
        }
    }
}