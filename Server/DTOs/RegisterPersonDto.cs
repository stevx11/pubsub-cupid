namespace HaoticniKupidon.Server.DTOs;

public record RegisterPersonDto(
	string Username,
	string City,
	int Age,
	string PhoneNumber
);