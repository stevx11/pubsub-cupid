namespace HaoticniKupidon.Server.DTOs;

public record LoveLetterDto(
    string SenderUsername,
    string SenderCity,
    int SenderAge,
    string? SenderPhone,   // null ako je poruka odbijanje
    string Message
);