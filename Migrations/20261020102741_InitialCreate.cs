using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MuseoApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ruoli",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ruoli", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sale",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Capienza = table.Column<int>(type: "int", nullable: false),
                    Ubicazione = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sale", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Utenti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Cognome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    RuoloId = table.Column<int>(type: "int", nullable: false),
                    DataRegistrazione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Attivo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utenti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Utenti_Ruoli_RuoloId",
                        column: x => x.RuoloId,
                        principalTable: "Ruoli",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Mostre",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titolo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Curatore = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DataInizio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataFine = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SalaId = table.Column<int>(type: "int", nullable: false),
                    Stato = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mostre", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mostre_Sale_SalaId",
                        column: x => x.SalaId,
                        principalTable: "Sale",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Opere",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titolo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Autore = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Anno = table.Column<int>(type: "int", nullable: true),
                    Tecnica = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Inventario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MostraId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Opere", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Opere_Mostre_MostraId",
                        column: x => x.MostraId,
                        principalTable: "Mostre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Turni",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MostraId = table.Column<int>(type: "int", nullable: false),
                    DataOra = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurataMinuti = table.Column<int>(type: "int", nullable: false),
                    PostiDisponibili = table.Column<int>(type: "int", nullable: false),
                    Stato = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turni", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Turni_Mostre_MostraId",
                        column: x => x.MostraId,
                        principalTable: "Mostre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Prenotazioni",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UtenteId = table.Column<int>(type: "int", nullable: false),
                    TurnoVisitaId = table.Column<int>(type: "int", nullable: false),
                    NumeroPosti = table.Column<int>(type: "int", nullable: false),
                    DataPrenotazione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Stato = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prenotazioni", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prenotazioni_Turni_TurnoVisitaId",
                        column: x => x.TurnoVisitaId,
                        principalTable: "Turni",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Prenotazioni_Utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Feedback",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrenotazioneId = table.Column<int>(type: "int", nullable: false),
                    Valutazione = table.Column<int>(type: "int", nullable: false),
                    Commento = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DataInserimento = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Feedback_Prenotazioni_PrenotazioneId",
                        column: x => x.PrenotazioneId,
                        principalTable: "Prenotazioni",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_PrenotazioneId",
                table: "Feedback",
                column: "PrenotazioneId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mostre_SalaId",
                table: "Mostre",
                column: "SalaId");

            migrationBuilder.CreateIndex(
                name: "IX_Opere_MostraId",
                table: "Opere",
                column: "MostraId");

            migrationBuilder.CreateIndex(
                name: "IX_Prenotazioni_TurnoVisitaId",
                table: "Prenotazioni",
                column: "TurnoVisitaId");

            migrationBuilder.CreateIndex(
                name: "IX_Prenotazioni_UtenteId_TurnoVisitaId",
                table: "Prenotazioni",
                columns: new[] { "UtenteId", "TurnoVisitaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ruoli_Nome",
                table: "Ruoli",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Turni_MostraId_DataOra",
                table: "Turni",
                columns: new[] { "MostraId", "DataOra" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Utenti_Email",
                table: "Utenti",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Utenti_RuoloId",
                table: "Utenti",
                column: "RuoloId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Feedback");

            migrationBuilder.DropTable(
                name: "Opere");

            migrationBuilder.DropTable(
                name: "Prenotazioni");

            migrationBuilder.DropTable(
                name: "Turni");

            migrationBuilder.DropTable(
                name: "Utenti");

            migrationBuilder.DropTable(
                name: "Mostre");

            migrationBuilder.DropTable(
                name: "Ruoli");

            migrationBuilder.DropTable(
                name: "Sale");
        }
    }
}
