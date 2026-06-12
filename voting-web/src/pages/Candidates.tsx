import { useEffect, useState } from "react";
import api from "../services/api";
import { FaUserTie } from "react-icons/fa";
import { useToast } from "../context/ToastContext";

interface Candidate {
  candidateId: string;
  candidateName: string;
  partyAffiliation?: string;
  description?: string;
  electionId: string;
}

interface Election {
  electionId: string;
  title: string;
  status: string | number;
}

const normalizeStatus = (status: string | number) => {
  if (typeof status === "number") {
    return ["Draft", "Active", "Closed"][status] ?? "Draft";
  }
  return status;
};

export default function Candidates() {
  const role =
    localStorage.getItem("role") ||
    "Voter";

  const canManage =
    role === "Admin" ||
    role === "ElectionOfficer";
  const canRemove =
    role === "Admin";

  const { showToast } = useToast();

  const [candidates, setCandidates] =
    useState<Candidate[]>([]);
  const [elections, setElections] =
    useState<Election[]>([]);

  const [form, setForm] = useState({
    electionId: "",
    name: "",
    partyAffiliation: "",
    description: "",
  });

  const fetchCandidates = async (
    electionId = form.electionId
  ) => {
    if (!electionId) {
      setCandidates([]);
      return;
    }

    try {
      const res = await api.get(
        `/elections/${electionId}/candidates`
      );

      setCandidates(
        res.data.map(
          (candidate: Omit<
            Candidate,
            "electionId"
          >) => ({
            ...candidate,
            electionId,
          })
        )
      );
    } catch (err) {
      console.error(err);
      setCandidates([]);
    }
  };

  const fetchElections = async () => {
    try {
      const res =
        await api.get("/elections");
      const allElections: Election[] =
        res.data.elections ?? [];
      const list = allElections.filter(
        (e) => normalizeStatus(e.status) !== "Closed"
      );

      setElections(list);

      if (list.length > 0) {
        const firstId =
          list[0].electionId;

        setForm((current) => ({
          ...current,
          electionId:
            current.electionId ||
            firstId,
        }));

        fetchCandidates(firstId);
      } else {
        setForm((current) => ({
          ...current,
          electionId: "",
        }));
        setCandidates([]);
      }
    } catch (err) {
      console.error(err);
    }
  };

  useEffect(() => {
    fetchElections();
  }, []);

  const createCandidate = async () => {
    if (!form.electionId) {
      showToast("Select an election first", "warning");
      return;
    }

    try {
      await api.post(
        `/elections/${form.electionId}/candidates`,
        {
          name: form.name,
          partyAffiliation:
            form.partyAffiliation,
          description:
            form.description,
        }
      );

      showToast(
        "Candidate Added Successfully",
        "success"
      );

      setForm((current) => ({
        ...current,
        name: "",
        partyAffiliation: "",
        description: "",
      }));

      fetchCandidates(form.electionId);
    } catch (err: any) {
      showToast(
        err?.response?.data
          ?.message ||
          "Failed to add candidate",
        "error"
      );
    }
  };

  const deleteCandidate = async (
    candidateId: string
  ) => {
    try {
      if (
        !window.confirm(
          "Delete this candidate?"
        )
      ) {
        return;
      }

      await api.delete(
        `/elections/${form.electionId}/candidates/${candidateId}`
      );

      showToast("Candidate Deleted", "success");
      fetchCandidates(form.electionId);
    } catch (err: any) {
      showToast(
        err?.response?.data
          ?.message ||
          "Delete Failed",
        "error"
      );
    }
  };

  return (
    <div>
      <h1 className="text-4xl font-bold mb-8">
        Candidates
      </h1>

      <div className="bg-slate-900 rounded-3xl p-6 mb-8">
        <h2 className="text-2xl font-semibold mb-5">
          Election Candidates
        </h2>

        <select
          className="w-full bg-slate-800 p-3 rounded-xl border border-slate-700"
          value={form.electionId}
          onChange={(e) => {
            const electionId =
              e.target.value;

            setForm({
              ...form,
              electionId,
            });
            fetchCandidates(
              electionId
            );
          }}
        >
          <option value="">
            Select Election
          </option>

          {elections.map((e) => (
            <option
              key={e.electionId}
              value={e.electionId}
            >
              {e.title}
            </option>
          ))}
        </select>
      </div>

      {canManage && (
        <div className="bg-slate-900 rounded-3xl p-6 mb-8">
          <h2 className="text-2xl font-semibold mb-5">
            Add Candidate
          </h2>

          <div className="grid md:grid-cols-2 gap-4">
            <input
              className="bg-slate-800 p-3 rounded-xl border border-slate-700"
              placeholder="Candidate Name"
              value={form.name}
              onChange={(e) =>
                setForm({
                  ...form,
                  name:
                    e.target.value,
                })
              }
            />

            <input
              className="bg-slate-800 p-3 rounded-xl border border-slate-700"
              placeholder="Party Affiliation"
              value={
                form.partyAffiliation
              }
              onChange={(e) =>
                setForm({
                  ...form,
                  partyAffiliation:
                    e.target.value,
                })
              }
            />

            <input
              className="bg-slate-800 p-3 rounded-xl border border-slate-700 md:col-span-2"
              placeholder="Description"
              value={form.description}
              onChange={(e) =>
                setForm({
                  ...form,
                  description:
                    e.target.value,
                })
              }
            />
          </div>

          <button
            onClick={createCandidate}
            className="mt-5 bg-cyan-500 text-black font-bold px-6 py-3 rounded-xl"
          >
            Add Candidate
          </button>
        </div>
      )}

      <div className="grid md:grid-cols-2 xl:grid-cols-3 gap-6">
        {candidates.map(
          (candidate) => (
            <div
              key={candidate.candidateId}
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

              {canRemove && (
                <button
                  onClick={() =>
                    deleteCandidate(
                      candidate.candidateId
                    )
                  }
                  className="w-full bg-red-600 hover:bg-red-500 text-white px-4 py-2 rounded-xl font-bold"
                >
                  Delete Candidate
                </button>
              )}
            </div>
          )
        )}
      </div>
    </div>
  );
}
