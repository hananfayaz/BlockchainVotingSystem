import { useEffect, useState } from "react";
import api from "../services/api";

import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  CartesianGrid,
} from "recharts";

import {
  FaTrophy,
  FaVoteYea,
  FaUsers,
  FaUniversity,
} from "react-icons/fa";

interface Election {
  electionId: string;
  title: string;
}

interface Result {
  candidate: string;
  votes: number;
}

export default function Results() {
  const [results, setResults] =
    useState<Result[]>([]);
  const [elections, setElections] =
    useState<Election[]>([]);
  const [selectedElection, setSelectedElection] =
    useState("");

  useEffect(() => {
    loadElections();
  }, []);

  useEffect(() => {
    if (selectedElection) {
      loadResults(selectedElection);
    }
  }, [selectedElection]);

  const loadElections = async () => {
    try {
      const res =
        await api.get("/elections");
      const list =
        res.data.elections ?? [];

      setElections(list);

      if (list.length > 0) {
        setSelectedElection(
          list[0].electionId
        );
      }
    } catch (err) {
      console.error(err);
    }
  };

  const loadResults = async (
    electionId: string
  ) => {
    try {
      const res = await api.get(
        `/results/${electionId}`
      );

      const formatted =
        res.data.map((item: any) => ({
          candidate:
            item.candidateName,
          votes:
            item.voteCount ?? 0,
        }));

      setResults(formatted);
    } catch (err) {
      console.error(err);
      setResults([]);
    }
  };

  const totalVotes = results.reduce(
    (sum, item) =>
      sum + item.votes,
    0
  );

  const winner =
    results.length > 0
      ? results.reduce(
          (prev, current) =>
            prev.votes >
            current.votes
              ? prev
              : current
        )
      : null;

  return (
    <div>
      <h1 className="text-4xl font-bold mb-8">
        Election Results
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
              </option>
            )
          )}
        </select>
      </div>

      <div className="grid md:grid-cols-3 gap-6 mb-8">
        <div className="bg-slate-900 rounded-3xl p-6">
          <div className="flex items-center gap-3 mb-3">
            <FaVoteYea
              className="text-cyan-400"
              size={28}
            />

            <h3 className="text-xl">
              Total Votes
            </h3>
          </div>

          <p className="text-4xl font-bold">
            {totalVotes}
          </p>
        </div>

        <div className="bg-slate-900 rounded-3xl p-6">
          <div className="flex items-center gap-3 mb-3">
            <FaUsers
              className="text-cyan-400"
              size={28}
            />

            <h3 className="text-xl">
              Candidates
            </h3>
          </div>

          <p className="text-4xl font-bold">
            {results.length}
          </p>
        </div>

        <div className="bg-slate-900 rounded-3xl p-6">
          <div className="flex items-center gap-3 mb-3">
            <FaTrophy
              className="text-yellow-400"
              size={28}
            />

            <h3 className="text-xl">
              Winner
            </h3>
          </div>

          <p className="text-xl font-bold text-green-400">
            {winner
              ? winner.candidate
              : "No Votes Yet"}
          </p>
        </div>
      </div>

      {winner && (
        <div className="bg-gradient-to-r from-yellow-500 to-orange-500 text-black rounded-3xl p-8 mb-8">
          <h2 className="text-3xl font-bold mb-3">
            Winning Candidate
          </h2>

          <p className="text-2xl font-semibold">
            {winner.candidate}
          </p>

          <p className="mt-2 text-lg">
            Total Votes: {winner.votes}
          </p>
        </div>
      )}

      <div className="bg-slate-900 rounded-3xl p-6">
        <h2 className="text-2xl font-bold mb-5">
          Vote Distribution
        </h2>

        <div
          style={{
            width: "100%",
            height: 450,
          }}
        >
          <ResponsiveContainer
            width="100%"
            height="100%"
          >
            <BarChart data={results} margin={{ top: 20, right: 30, left: 0, bottom: 5 }}>
              <defs>
                <linearGradient id="voteGradient" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#22d3ee" stopOpacity={1} />
                  <stop offset="100%" stopColor="#0284c7" stopOpacity={0.4} />
                </linearGradient>
              </defs>
              <CartesianGrid stroke="#1e293b" strokeDasharray="3 3" vertical={false} />
              <XAxis 
                dataKey="candidate" 
                stroke="#64748b" 
                tick={{ fill: "#94a3b8", fontSize: 13 }} 
                axisLine={{ stroke: "#334155" }}
                tickLine={{ stroke: "#334155" }}
              />
              <YAxis 
                stroke="#64748b" 
                tick={{ fill: "#94a3b8", fontSize: 13 }} 
                axisLine={{ stroke: "#334155" }}
                tickLine={{ stroke: "#334155" }}
                allowDecimals={false}
              />
              <Tooltip 
                contentStyle={{
                  backgroundColor: "#0f172a",
                  border: "1px solid #334155",
                  borderRadius: "12px",
                  color: "#fff",
                }}
                itemStyle={{ color: "#22d3ee" }}
                labelStyle={{ fontWeight: "bold", color: "#94a3b8" }}
              />
              <Bar
                dataKey="votes"
                fill="url(#voteGradient)"
                radius={[8, 8, 0, 0]}
                maxBarSize={60}
              />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>
    </div>
  );
}
