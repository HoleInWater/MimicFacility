// MimicFacilityHUD.cpp — HUD with canvas-drawn round info and Director message display.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "MimicFacilityHUD.h"
#include "Networking/MimicFacilityGameState.h"
#include "Engine/Canvas.h"
#include "Engine/Font.h"

AMimicFacilityHUD::AMimicFacilityHUD()
{
	DirectorMessageDuration = 8.0f;
	DirectorMessageTimer = 0.0f;
}

void AMimicFacilityHUD::BeginPlay()
{
	Super::BeginPlay();
}

void AMimicFacilityHUD::ShowDirectorMessage(const FString& Message)
{
	CurrentDirectorMessage = Message;
	DirectorMessageTimer = DirectorMessageDuration;
}

void AMimicFacilityHUD::DrawHUD()
{
	Super::DrawHUD();

	if (!Canvas) return;

	const float ScreenW = Canvas->SizeX;
	const float ScreenH = Canvas->SizeY;

	// Get game state for data
	AMimicFacilityGameState* GS = Cast<AMimicFacilityGameState>(GetWorld()->GetGameState());

	// --- Top-left: Round info ---
	FString RoundText = TEXT("Round: --");
	FString MimicText = TEXT("Mimics: --");
	FString ContainedText = TEXT("Contained: --");

	if (GS)
	{
		RoundText = FString::Printf(TEXT("Round: %d"), GS->GetCurrentRound());
		MimicText = FString::Printf(TEXT("Active Mimics: %d"), GS->GetActiveMimicCount());
		ContainedText = FString::Printf(TEXT("Contained: %d"), GS->GetContainedMimicCount());
	}

	// Draw background panel
	FLinearColor PanelColor(0.0f, 0.0f, 0.0f, 0.6f);
	Canvas->DrawTile(Canvas->DefaultTexture, 10, 10, 200, 80, 0, 0, 1, 1, PanelColor);

	// Draw text
	FLinearColor TextColor = FLinearColor::White;
	Canvas->DrawText(GEngine->GetSmallFont(), RoundText, 20, 20, 1.2f, 1.2f, FLinearColor::Green);
	Canvas->DrawText(GEngine->GetSmallFont(), MimicText, 20, 40, 1.0f, 1.0f, FLinearColor::Red);
	Canvas->DrawText(GEngine->GetSmallFont(), ContainedText, 20, 55, 1.0f, 1.0f, FLinearColor::Yellow);

	// --- Bottom-center: Director message ---
	if (DirectorMessageTimer > 0.0f)
	{
		DirectorMessageTimer -= GetWorld()->GetDeltaSeconds();

		float Alpha = FMath::Clamp(DirectorMessageTimer / 2.0f, 0.0f, 1.0f); // Fade out over last 2 seconds

		FString DisplayText = FString::Printf(TEXT("[DIRECTOR]: %s"), *CurrentDirectorMessage);

		float TextW, TextH;
		Canvas->TextSize(GEngine->GetMediumFont(), DisplayText, TextW, TextH);

		float PosX = (ScreenW - TextW) * 0.5f;
		float PosY = ScreenH - 120.0f;

		// Background
		FLinearColor MsgBG(0.0f, 0.0f, 0.0f, 0.7f * Alpha);
		Canvas->DrawTile(Canvas->DefaultTexture, PosX - 10, PosY - 5, TextW + 20, TextH + 10, 0, 0, 1, 1, MsgBG);

		// Text
		FLinearColor MsgColor(0.7f, 1.0f, 0.7f, Alpha);
		Canvas->DrawText(GEngine->GetMediumFont(), DisplayText, PosX, PosY, 1.0f, 1.0f, MsgColor);
	}

	// --- Center: Crosshair ---
	float CenterX = ScreenW * 0.5f;
	float CenterY = ScreenH * 0.5f;
	float CrossSize = 8.0f;

	Canvas->Draw2DLine(CenterX - CrossSize, CenterY, CenterX + CrossSize, CenterY, FLinearColor::White);
	Canvas->Draw2DLine(CenterX, CenterY - CrossSize, CenterX, CenterY + CrossSize, FLinearColor::White);
}
