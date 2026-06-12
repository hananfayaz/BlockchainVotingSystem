import { useEffect, useState } from "react";
import { ethers } from "ethers";
import api from "../services/api";
import {
  FaVoteYea,
  FaUserTie,
  FaUniversity,
  FaCheckCircle,
} from "react-icons/fa";
import { useToast } from "../context/ToastContext";

interface Candidate {
  candidateId: string;
  candidateName: string;
  partyAffiliation?: string;
  description?: string;
}

interface Election {
  electionId: string;
  title: string;
  status: string | number;
  hasVoted: boolean;
}

interface VotePrepareResponse {
  contractAddress: string;
  candidateIndex: number;
}

const votingAbi = [
  "function vote(address voter, uint256 candidateId) external",
];

const normalizeStatus = (status: string | number) => {
  if (typeof status === "number") {
    return ["Draft", "Active", "Closed"][status] ?? "Draft";
  }
  return status;
};

const normalizeElection = (e: any): Election => ({
  electionId: e.electionId ?? e.ElectionId ?? "",
  title: e.title ?? e.Title ?? "",
  status: e.status ?? e.Status ?? 0,
  hasVoted: e.hasVoted ?? e.HasVoted ?? false,
});

export default function Vote() {
  const [candidates, setCandidates] =
    useState<Candidate[]>([]);
  const [elections, setElections] =
    useState<Election[]>([]);
  const [selectedElection, setSelectedElection] =
    useState("");
  const [loadingCandidateId, setLoadingCandidateId] =
    useState("");

  const { showToast } = useToast();
  const [sendingOtpCandidateId, setSendingOtpCandidateId] =
    useState("");
  const [pendingVoteCandidateId, setPendingVoteCandidateId] =
    useState("");
  const [otp, setOtp] = useState("");

  useEffect(() => {
    loadElections();
  }, []);

  useEffect(() => {
    if (selectedElection) {
      loadCandidates(
        selectedElection
      );
    }
  }, [selectedElection]);

  const loadElections = async () => {
    try {
      const res =
        await api.get("/elections");
      const allElections: Election[] =
        (res.data.elections ?? []).map(normalizeElection);

      // Only show active elections (not closed, not draft)
      const list = allElections.filter(
        (e) => normalizeStatus(e.status) === "Active"
      );

      setElections(list);

      if (list.length > 0) {
        setSelectedElection(
          list[0].electionId
        );
      } else {
        setSelectedElection("");
        setCandidates([]);
      }
    } catch (err) {
      console.error(err);
    }
  };

  const loadCandidates = async (
    electionId: string
  ) => {
    try {
      const res = await api.get(
        `/elections/${electionId}/candidates`
      );

      setCandidates(res.data);
    } catch (err) {
      console.error(err);
      setCandidates([]);
    }
  };

  // Check if the currently selected election has been voted
  const currentElection = elections.find(
    (e) => e.electionId === selectedElection
  );
  const hasVotedInSelected = currentElection?.hasVoted ?? false;

  const requestVoteOtp = async (
    candidateId: string
  ) => {
    try {
      setSendingOtpCandidateId(
        candidateId
      );

      await api.post(
        "/vote/send-otp",
        {
          electionId:
            selectedElection,
          candidateId,
        }
      );

      setPendingVoteCandidateId(
        candidateId
      );
      setOtp("");

      showToast(
        "OTP sent to your registered email.",
        "info"
      );
    } catch (err: any) {
      console.error(err);

      const message =
        err?.response?.data
          ?.message ||
        "Failed to send vote OTP";

      showToast(
        message ===
          "Not registered for election"
          ? "You are not registered for this election. Ask an admin or election officer to register you before voting."
          : message,
        "error"
      );
    } finally {
      setSendingOtpCandidateId("");
    }
  };

  const submitVerifiedVote = async (
    event: React.FormEvent
  ) => {
    event.preventDefault();

    if (!pendingVoteCandidateId) {
      return;
    }

    try {
      if (!window.ethereum) {
        showToast(
          "Please install MetaMask to vote.",
          "warning"
        );
        return;
      }

      setLoadingCandidateId(
        pendingVoteCandidateId
      );

      const prepared = await api.post(
        "/vote/prepare",
        {
          electionId:
            selectedElection,
          candidateId:
            pendingVoteCandidateId,
          otp,
        }
      );

      const voteData: VotePrepareResponse =
        prepared.data.message;
      const provider =
        new ethers.BrowserProvider(
          window.ethereum
        );
      const signer =
        await provider.getSigner();
      const voterAddress =
        await signer.getAddress();
      const contract =
        new ethers.Contract(
          voteData.contractAddress,
          votingAbi,
          signer
        );

      const tx =
        await contract.vote(
          voterAddress,
          voteData.candidateIndex
        );
      const receipt =
        await tx.wait();

      await api.post(
        "/vote/confirm",
        {
          electionId:
            selectedElection,
          candidateId:
            pendingVoteCandidateId,
          txHash: receipt.hash,
          blockNumber:
            Number(
              receipt.blockNumber
            ),
        }
      );

      showToast(
        "Vote cast successfully!",
        "success"
      );

      // Mark this election as voted in local state
      setElections((prev) =>
        prev.map((e) =>
          e.electionId === selectedElection
            ? { ...e, hasVoted: true }
            : e
        )
      );

      setPendingVoteCandidateId("");
      setOtp("");
    } catch (err: any) {
      console.error(err);

      showToast(
        err?.response?.data
          ?.message ||
          err?.shortMessage ||
          err?.message ||
          "Vote failed",
        "error"
      );
    } finally {
      setLoadingCandidateId("");
    }
  };

  return (
    <div>
      <h1 className="text-4xl font-bold mb-8">
        Cast Your Vote
      </h1>

      <div className="bg-slate-900 rounded-3xl p-6 mb-8">
        <div className="flex items-center gap-3 mb-4">
          <FaUniversity
            className="text-cyan-400"
            size={24}
          />

          <h2 className="text-xl font-bold">
            Select Election
          </h2>
        </div>

        <select
          value={selectedElection}
          onChange={(e) =>
            setSelectedElection(
              e.target.value
            )
          }
          className="w-full bg-slate-800 border border-slate-700 rounded-xl p-3"
        >
          {elections.length === 0 && (
            <option value="">
              No active elections
            </option>
          )}
          {elections.map(
            (election) => (
              <option
                key={
                  election.electionId
                }
                value={
                  election.electionId
                }
              >
                {election.title}
                {election.hasVoted
                  ? " (Voted)"
                  : ""}
              </option>
            )
          )}
        </select>
      </div>

      {hasVotedInSelected && (
        <div className="bg-green-500/10 border border-green-500/30 rounded-3xl p-6 mb-8 flex items-center gap-4">
          <FaCheckCircle
            className="text-green-400"
            size={28}
          />
          <div>
            <h3 className="text-lg font-bold text-green-400">
              You have successfully voted in this election
            </h3>
            <p className="text-slate-400 text-sm">
              Your vote has been recorded on the blockchain.
            </p>
          </div>
        </div>
      )}

      <div className="grid md:grid-cols-2 xl:grid-cols-3 gap-6">
        {candidates.map(
          (candidate) => (
            <div
              key={
                candidate.candidateId
              }
              className="bg-slate-900 rounded-3xl p-6 shadow-lg hover:scale-105 transition"
            >
              <div className="flex items-center gap-3 mb-4">
                <FaUserTie
                  size={32}
                  className="text-cyan-400"
                />

                <div>
                  <h3 className="text-xl font-bold">
                    {
                      candidate.candidateName
                    }
                  </h3>

                  <p className="text-slate-400">
                    {candidate.partyAffiliation ||
                      "Independent"}
                  </p>
                </div>
              </div>

              <p className="mb-4 text-slate-300">
                {candidate.description ||
                  "No description provided."}
              </p>

              {hasVotedInSelected ? (
                <div className="w-full bg-green-500/15 border border-green-500/30 text-green-400 font-bold py-3 rounded-xl flex items-center justify-center gap-2">
                  <FaCheckCircle />
                  Successfully Voted
                </div>
              ) : (
                <button
                  onClick={() =>
                    requestVoteOtp(
                      candidate.candidateId
                    )
                  }
                  disabled={
                    loadingCandidateId ===
                      candidate.candidateId ||
                    sendingOtpCandidateId ===
                    candidate.candidateId
                  }
                  className="w-full bg-cyan-500 hover:bg-cyan-400 text-black font-bold py-3 rounded-xl flex items-center justify-center gap-2"
                >
                  <FaVoteYea />
                  {loadingCandidateId ===
                  candidate.candidateId
                    ? "Voting..."
                    : sendingOtpCandidateId ===
                      candidate.candidateId
                    ? "Sending OTP..."
                    : "Vote Now"}
                </button>
              )}
            </div>
          )
        )}
      </div>

      {pendingVoteCandidateId && (
        <div className="fixed inset-0 bg-black/70 flex items-center justify-center p-6 z-50">
          <div className="w-full max-w-md bg-slate-900 rounded-3xl p-6 border border-slate-700 shadow-2xl">
            <h2 className="text-2xl font-bold mb-4 text-cyan-400">
              Verify Vote OTP
            </h2>

            <form onSubmit={submitVerifiedVote}>
              <input
                type="text"
                required
                placeholder="Enter OTP"
                value={otp}
                onChange={(e) =>
                  setOtp(e.target.value)
                }
                className="w-full bg-slate-800 border border-slate-700 rounded-xl p-3 mb-4"
              />

              <div className="grid grid-cols-2 gap-3">
                <button
                  type="button"
                  onClick={() => {
                    setPendingVoteCandidateId(
                      ""
                    );
                    setOtp("");
                  }}
                  disabled={
                    Boolean(
                      loadingCandidateId
                    )
                  }
                  className="bg-slate-700 hover:bg-slate-600 text-white font-bold py-3 rounded-xl"
                >
                  Cancel
                </button>

                <button
                  type="submit"
                  disabled={
                    Boolean(
                      loadingCandidateId
                    )
                  }
                  className="bg-cyan-500 hover:bg-cyan-400 disabled:bg-slate-600 disabled:text-slate-300 text-black font-bold py-3 rounded-xl"
                >
                  {loadingCandidateId
                    ? "Voting..."
                    : "Verify & Vote"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {candidates.length === 0 && (
        <div className="bg-slate-900 rounded-3xl p-10 text-center text-slate-400 mt-6">
          No candidates found for this election.
        </div>
      )}
    </div>
  );
}