namespace Antital.Application.DTOs.Onboarding;

public record FundRaiserBusinessDocumentsPayload(
    string FounderAndTeamIntroductionDocumentPathOrKey,
    string FundraisingDeckDocumentPathOrKey,
    string InvestmentMemoDocumentPathOrKey,
    string TermsOfOfferingDocumentPathOrKey,
    string? ProductDemoDocumentPathOrKey,
    string BusinessDescription,
    string BusinessSector,
    string InstrumentType,
    string BusinessSize,
    decimal? FundingTarget,
    string InvestmentRound
);
