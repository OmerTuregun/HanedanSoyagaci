using MySqlConnector;
using FamilyTreeApp.Models;
using Newtonsoft.Json;
using SkiaSharp;

namespace FamilyTreeApp.Services;

public class DataService
{
    private readonly string _connectionString;

    public DataService(string connectionString)
    {
        _connectionString = connectionString;
    }

    // Connection String için varsayılan değer
    // Production: Sunucu IP'si ve port'u buraya yazın
    public static string DefaultConnectionString => 
        "Server=194.146.50.83;Database=familytree;User=familytree_user;Password=FamilyTreeUser2024!;Port=3308;CharSet=utf8mb4;";
    
    // Local development için alternatif
    public static string LocalConnectionString => 
        "Server=localhost;Database=familytree;User=root;Password=;Port=3306;CharSet=utf8mb4;";

    // Veritabanı tablolarını oluştur (ilk çalıştırmada)
    public async Task InitializeDatabaseAsync()
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Persons tablosu
            var createPersonsTable = @"
                CREATE TABLE IF NOT EXISTS Persons (
                    Id VARCHAR(36) PRIMARY KEY,
                    Name VARCHAR(255) NOT NULL,
                    Gender INT NOT NULL,
                    Reigns TEXT,
                    FillColor TEXT,
                    BorderColor INT NOT NULL DEFAULT 0,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

            // Relationships tablosu
            var createRelationshipsTable = @"
                CREATE TABLE IF NOT EXISTS Relationships (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    FromPersonId VARCHAR(36) NOT NULL,
                    ToPersonId VARCHAR(36) NOT NULL,
                    Type INT NOT NULL,
                    IsUncertain BOOLEAN DEFAULT FALSE,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (FromPersonId) REFERENCES Persons(Id) ON DELETE CASCADE,
                    FOREIGN KEY (ToPersonId) REFERENCES Persons(Id) ON DELETE CASCADE,
                    INDEX idx_from (FromPersonId),
                    INDEX idx_to (ToPersonId)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

            using var cmd1 = new MySqlCommand(createPersonsTable, connection);
            await cmd1.ExecuteNonQueryAsync();

            using var cmd2 = new MySqlCommand(createRelationshipsTable, connection);
            await cmd2.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Veritabanı başlatma hatası: {ex.Message}", ex);
        }
    }

    // Tüm ağacı yükle
    public async Task<(List<Person> People, List<Relationship> Relationships)> LoadTreeAsync()
    {
        var people = new List<Person>();
        var relationships = new List<Relationship>();

        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Persons yükle
            var personsQuery = "SELECT Id, Name, Gender, Reigns, FillColor, BorderColor FROM Persons";
            using var personsCmd = new MySqlCommand(personsQuery, connection);
            using var personsReader = await personsCmd.ExecuteReaderAsync();

            while (await personsReader.ReadAsync())
            {
                var person = new Person
                {
                    Id = personsReader.GetString("Id"),
                    Name = personsReader.GetString("Name"),
                    Gender = (Gender)personsReader.GetInt32("Gender")
                };

                // Reigns (JSON array)
                int reignsOrdinal = personsReader.GetOrdinal("Reigns");
                if (!personsReader.IsDBNull(reignsOrdinal))
                {
                    var reignsJson = personsReader.GetString(reignsOrdinal);
                    if (!string.IsNullOrEmpty(reignsJson))
                    {
                        person.Reigns = JsonConvert.DeserializeObject<List<string>>(reignsJson) ?? new List<string>();
                    }
                }

                // FillColor (JSON - SKColor veya List<SKColor>)
                int fillColorOrdinal = personsReader.GetOrdinal("FillColor");
                if (!personsReader.IsDBNull(fillColorOrdinal))
                {
                    var fillColorJson = personsReader.GetString(fillColorOrdinal);
                    if (!string.IsNullOrEmpty(fillColorJson))
                    {
                        try
                        {
                            // Önce List<SKColor> olarak dene
                            var colorList = JsonConvert.DeserializeObject<List<uint>>(fillColorJson);
                            if (colorList != null && colorList.Count > 0)
                            {
                                if (colorList.Count == 1)
                                {
                                    person.FillColor = new SKColor(colorList[0]);
                                }
                                else
                                {
                                    person.FillColor = colorList.Select(c => new SKColor(c)).ToList();
                                }
                            }
                        }
                        catch
                        {
                            // Tek renk olarak dene
                            try
                            {
                                var colorValue = JsonConvert.DeserializeObject<uint>(fillColorJson);
                                person.FillColor = new SKColor(colorValue);
                            }
                            catch
                            {
                                // Varsayılan renk
                                person.FillColor = SKColors.LightGray;
                            }
                        }
                    }
                }

                // BorderColor
                int borderColorOrdinal = personsReader.GetOrdinal("BorderColor");
                if (!personsReader.IsDBNull(borderColorOrdinal))
                {
                    person.BorderColor = new SKColor((uint)personsReader.GetInt64(borderColorOrdinal));
                }

                people.Add(person);
            }

            await personsReader.CloseAsync();

            // Relationships yükle
            var relationshipsQuery = "SELECT FromPersonId, ToPersonId, Type, IsUncertain FROM Relationships";
            using var relsCmd = new MySqlCommand(relationshipsQuery, connection);
            using var relsReader = await relsCmd.ExecuteReaderAsync();

            while (await relsReader.ReadAsync())
            {
                var relationship = new Relationship
                {
                    FromPersonId = relsReader.GetString("FromPersonId"),
                    ToPersonId = relsReader.GetString("ToPersonId"),
                    Type = (RelationshipType)relsReader.GetInt32("Type"),
                    IsUncertain = relsReader.GetBoolean("IsUncertain")
                };

                relationships.Add(relationship);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Ağaç yükleme hatası: {ex.Message}", ex);
        }

        return (people, relationships);
    }

    // Kişi kaydet
    public async Task SavePersonAsync(Person person)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var reignsJson = JsonConvert.SerializeObject(person.Reigns ?? new List<string>());
            
            // FillColor'ı serialize et
            string fillColorJson = "";
            if (person.FillColor is SKColor singleColor)
            {
                fillColorJson = JsonConvert.SerializeObject((uint)singleColor);
            }
            else if (person.FillColor is List<SKColor> colorList)
            {
                fillColorJson = JsonConvert.SerializeObject(colorList.Select(c => (uint)c).ToList());
            }

            var query = @"
                INSERT INTO Persons (Id, Name, Gender, Reigns, FillColor, BorderColor)
                VALUES (@Id, @Name, @Gender, @Reigns, @FillColor, @BorderColor)
                ON DUPLICATE KEY UPDATE
                    Name = @Name,
                    Gender = @Gender,
                    Reigns = @Reigns,
                    FillColor = @FillColor,
                    BorderColor = @BorderColor,
                    UpdatedAt = CURRENT_TIMESTAMP";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", person.Id);
            cmd.Parameters.AddWithValue("@Name", person.Name);
            cmd.Parameters.AddWithValue("@Gender", (int)person.Gender);
            cmd.Parameters.AddWithValue("@Reigns", reignsJson);
            cmd.Parameters.AddWithValue("@FillColor", fillColorJson);
            cmd.Parameters.AddWithValue("@BorderColor", (long)(uint)person.BorderColor);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Kişi kaydetme hatası: {ex.Message}", ex);
        }
    }

    // Kişi güncelle
    public async Task UpdatePersonAsync(Person person)
    {
        await SavePersonAsync(person); // Aynı mantık (INSERT ... ON DUPLICATE KEY UPDATE)
    }

    // Kişi sil
    public async Task DeletePersonAsync(Person person)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "DELETE FROM Persons WHERE Id = @Id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", person.Id);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Kişi silme hatası: {ex.Message}", ex);
        }
    }

    // İlişki kaydet
    public async Task SaveRelationshipAsync(Relationship relationship)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                INSERT INTO Relationships (FromPersonId, ToPersonId, Type, IsUncertain)
                VALUES (@FromPersonId, @ToPersonId, @Type, @IsUncertain)
                ON DUPLICATE KEY UPDATE
                    Type = @Type,
                    IsUncertain = @IsUncertain";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@FromPersonId", relationship.FromPersonId);
            cmd.Parameters.AddWithValue("@ToPersonId", relationship.ToPersonId);
            cmd.Parameters.AddWithValue("@Type", (int)relationship.Type);
            cmd.Parameters.AddWithValue("@IsUncertain", relationship.IsUncertain);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"İlişki kaydetme hatası: {ex.Message}", ex);
        }
    }

    // İlişki sil
    public async Task DeleteRelationshipAsync(Relationship relationship)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                DELETE FROM Relationships 
                WHERE FromPersonId = @FromPersonId 
                AND ToPersonId = @ToPersonId 
                AND Type = @Type";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@FromPersonId", relationship.FromPersonId);
            cmd.Parameters.AddWithValue("@ToPersonId", relationship.ToPersonId);
            cmd.Parameters.AddWithValue("@Type", (int)relationship.Type);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"İlişki silme hatası: {ex.Message}", ex);
        }
    }
}

