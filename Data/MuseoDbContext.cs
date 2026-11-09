using Microsoft.EntityFrameworkCore;
using MuseoApi.Models;

namespace MuseoApi.Data;

public class MuseoDbContext : DbContext
{
    public MuseoDbContext(DbContextOptions<MuseoDbContext> options) : base(options)
    {
    }

    public DbSet<Ruolo> Ruoli => Set<Ruolo>();
    public DbSet<Utente> Utenti => Set<Utente>();
    public DbSet<Sala> Sale => Set<Sala>();
    public DbSet<Mostra> Mostre => Set<Mostra>();
    public DbSet<TurnoVisita> Turni => Set<TurnoVisita>();
    public DbSet<Prenotazione> Prenotazioni => Set<Prenotazione>();
    public DbSet<Feedback> Feedback => Set<Feedback>();
    public DbSet<Opera> Opere => Set<Opera>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ruolo>(e =>
        {
            e.Property(r => r.Nome).IsRequired().HasMaxLength(50);
            e.HasIndex(r => r.Nome).IsUnique();
        });

        modelBuilder.Entity<Utente>(e =>
        {
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256);
            e.Property(u => u.Nome).IsRequired().HasMaxLength(100);
            e.Property(u => u.Cognome).IsRequired().HasMaxLength(100);
            e.Property(u => u.Telefono).HasMaxLength(30);
            e.HasIndex(u => u.Email).IsUnique();

            // Restrict: un utente con prenotazioni non deve sparire per cascata.
            e.HasOne(u => u.Ruolo)
                .WithMany(r => r.Utenti)
                .HasForeignKey(u => u.RuoloId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Sala>(e =>
        {
            e.Property(s => s.Nome).IsRequired().HasMaxLength(100);
            e.Property(s => s.Ubicazione).HasMaxLength(200);
        });

        modelBuilder.Entity<Opera>(e =>
        {
            e.Property(o => o.Titolo).IsRequired().HasMaxLength(300);
            e.Property(o => o.Autore).IsRequired().HasMaxLength(200);
            e.Property(o => o.Tecnica).HasMaxLength(150);
            e.Property(o => o.Inventario).HasMaxLength(50);

            // Smontata la mostra, le opere tornano alla collezione permanente.
            e.HasOne(o => o.Mostra)
                .WithMany(m => m.Opere)
                .HasForeignKey(o => o.MostraId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Mostra>(e =>
        {
            e.Property(m => m.Titolo).IsRequired().HasMaxLength(200);
            e.Property(m => m.Descrizione).HasMaxLength(2000);
            e.Property(m => m.Curatore).HasMaxLength(150);
            e.Property(m => m.Stato).HasConversion<int>();

            e.HasOne(m => m.Sala)
                .WithMany(s => s.Mostre)
                .HasForeignKey(m => m.SalaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TurnoVisita>(e =>
        {
            e.Property(t => t.Stato).HasConversion<int>();

            // Due turni non possono partire nello stesso istante per la stessa mostra.
            e.HasIndex(t => new { t.MostraId, t.DataOra }).IsUnique();

            // Cascade: i turni non hanno senso senza la loro mostra.
            e.HasOne(t => t.Mostra)
                .WithMany(m => m.Turni)
                .HasForeignKey(t => t.MostraId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Prenotazione>(e =>
        {
            e.Property(p => p.Stato).HasConversion<int>();

            // Vincolo di unicita: una sola prenotazione per utente su ogni turno.
            e.HasIndex(p => new { p.UtenteId, p.TurnoVisitaId }).IsUnique();

            e.HasOne(p => p.Utente)
                .WithMany(u => u.Prenotazioni)
                .HasForeignKey(p => p.UtenteId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.TurnoVisita)
                .WithMany(t => t.Prenotazioni)
                .HasForeignKey(p => p.TurnoVisitaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Feedback>(e =>
        {
            e.Property(f => f.Commento).HasMaxLength(1000);

            // Relazione uno a uno: un feedback per prenotazione, non di piu.
            e.HasIndex(f => f.PrenotazioneId).IsUnique();

            e.HasOne(f => f.Prenotazione)
                .WithOne(p => p.Feedback)
                .HasForeignKey<Feedback>(f => f.PrenotazioneId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
