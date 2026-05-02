using Microsoft.EntityFrameworkCore;
using VotingAPI.Models.Entities;

namespace VotingAPI.Data
{
    public class VotingDbContext : DbContext
    {
        public VotingDbContext(DbContextOptions options) : base(options) {}

        public DbSet<User> Users { get; set; }
        public DbSet<Election> Elections { get; set; }
        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<Voter> Voters { get; set; }
        public DbSet<VoteRecord> VoteRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── User ────────────────────────────────────────────────────

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique(); // Ensure email uniqueness at the database level

            // User → Elections (created by this user)
            modelBuilder.Entity<User>()
                .HasMany(u => u.CreatedElections)
                .WithOne(e => e.Creator)
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict); // User deletes should not auto-delete elections, elections are permanent records

            // User → Voters
            modelBuilder.Entity<User>()
                .HasMany(u => u.Voters)
                .WithOne(v => v.User)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Voter table is already deleted via Election cascade, so User must not also cascade delete it

            // User → VoteRecords
            modelBuilder.Entity<User>()
                .HasMany(u => u.VoteRecords)
                .WithOne(vr => vr.User)
                .HasForeignKey(vr => vr.UserId)
                .OnDelete(DeleteBehavior.Restrict); // VoteRecords are permanent audit logs, never auto-delete

            // ── Election ─────────────────────────────────────────────────

            // Election → Candidates
            modelBuilder.Entity<Election>()
                .HasMany(e => e.Candidates)
                .WithOne(c => c.Election)
                .HasForeignKey(c => c.ElectionId);

            // Election → Voters
            modelBuilder.Entity<Election>()
                .HasMany(e => e.Voters)
                .WithOne(v => v.Election)
                .HasForeignKey(v => v.ElectionId);

            // Election → VoteRecords
            modelBuilder.Entity<Election>()
                .HasMany(e => e.VoteRecords)
                .WithOne(vr => vr.Election)
                .HasForeignKey(vr => vr.ElectionId)
                .OnDelete(DeleteBehavior.Restrict); // VoteRecords are permanent audit logs, never auto-delete, even if election is deleted

            // ── Candidate ────────────────────────────────────────────────

            // Candidate → VoteRecords
            modelBuilder.Entity<Candidate>()
                .HasMany(c => c.VoteRecords)
                .WithOne(vr => vr.Candidate)
                .HasForeignKey(vr => vr.CandidateId)
                .OnDelete(DeleteBehavior.Restrict); // VoteRecords are permanent audit logs, never auto-delete, even if candidate is deleted

            // ── Voter ────────────────────────────────────────────────────

            modelBuilder.Entity<Voter>()
                .HasIndex(v => new { v.ElectionId, v.UserId })
                .IsUnique(); // Ensure a user can only be a voter once per election (one voter per election)

            // ── VoteRecord ───────────────────────────────────────────────

            modelBuilder.Entity<VoteRecord>()
                .HasIndex(vr => new { vr.ElectionId, vr.UserId })
                .IsUnique(); // Ensure a user can only vote once per election (one vote record per election per user) => (one vote per user per election)
        }
    }
}