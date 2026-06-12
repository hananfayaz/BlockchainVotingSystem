import { useState } from "react";
import api from "../services/api";
import { FaUserShield } from "react-icons/fa";
import { useToast } from "../context/ToastContext";

export default function ElectionOfficers() {
  const [form, setForm] = useState({
    fullName: "",
    email: "",
    password: "",
  });
  const [loading, setLoading] =
    useState(false);

  const { showToast } = useToast();

  const createOfficer = async (
    event: React.FormEvent
  ) => {
    event.preventDefault();

    try {
      setLoading(true);

      await api.post(
        "/Admin/create-officer",
        form
      );

      showToast(
        "Election officer created successfully. OTP sent to their email.",
        "success"
      );

      setForm({
        fullName: "",
        email: "",
        password: "",
      });
    } catch (error: any) {
      showToast(
        error?.response?.data
          ?.message ||
          "Failed to create election officer",
        "error"
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <h1 className="text-4xl font-bold mb-8">
        Election Officers
      </h1>

      <div className="bg-slate-900 rounded-3xl p-6 mb-8 shadow-lg">
        <div className="flex items-center gap-3 mb-5">
          <FaUserShield
            size={28}
            className="text-cyan-400"
          />

          <h2 className="text-2xl font-semibold">
            Create Election Officer
          </h2>
        </div>

        <form onSubmit={createOfficer}>
          <div className="grid md:grid-cols-2 gap-4">
            <input
              type="text"
              required
              className="bg-slate-800 p-3 rounded-xl border border-slate-700"
              placeholder="Full Name"
              value={form.fullName}
              onChange={(event) =>
                setForm({
                  ...form,
                  fullName:
                    event.target.value,
                })
              }
            />

            <input
              type="email"
              required
              className="bg-slate-800 p-3 rounded-xl border border-slate-700"
              placeholder="Email Address"
              value={form.email}
              onChange={(event) =>
                setForm({
                  ...form,
                  email:
                    event.target.value,
                })
              }
            />

            <input
              type="password"
              required
              className="bg-slate-800 p-3 rounded-xl border border-slate-700 md:col-span-2"
              placeholder="Temporary Password"
              value={form.password}
              onChange={(event) =>
                setForm({
                  ...form,
                  password:
                    event.target.value,
                })
              }
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            className="mt-5 bg-cyan-500 hover:bg-cyan-400 disabled:bg-slate-600 disabled:text-slate-300 text-black font-bold px-6 py-3 rounded-xl"
          >
            {loading
              ? "Creating Officer..."
              : "Create Officer"}
          </button>
        </form>
      </div>

      <div className="bg-slate-900 rounded-3xl p-6 text-slate-300">
        New election officers are created with the ElectionOfficer role and must verify their email OTP before logging in.
      </div>
    </div>
  );
}
