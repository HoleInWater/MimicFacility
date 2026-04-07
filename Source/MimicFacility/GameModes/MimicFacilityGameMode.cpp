// MimicFacilityGameMode.cpp — Primary game mode. Spawns Director/RoundManager and assigns subject numbers.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "MimicFacilityGameMode.h"
#include "Characters/MimicFacilityCharacter.h"
#include "Networking/MimicFacilityGameState.h"
#include "Networking/MimicFacilityPlayerState.h"
#include "GameModes/RoundManager.h"
#include "AI/DirectorAI.h"
#include "UI/MimicFacilityHUD.h"

AMimicFacilityGameMode::AMimicFacilityGameMode()
{
	DefaultPawnClass = AMimicFacilityCharacter::StaticClass();
	HUDClass = AMimicFacilityHUD::StaticClass();
	GameStateClass = AMimicFacilityGameState::StaticClass();
	PlayerStateClass = AMimicFacilityPlayerState::StaticClass();
	NextSubjectNumber = 1;
}

void AMimicFacilityGameMode::InitGame(const FString& MapName, const FString& Options, FString& ErrorMessage)
{
	Super::InitGame(MapName, Options, ErrorMessage);
	UE_LOG(LogTemp, Log, TEXT("MimicFacilityGameMode::InitGame — Map: %s"), *MapName);
}

void AMimicFacilityGameMode::BeginPlay()
{
	Super::BeginPlay();

	if (HasAuthority())
	{
		FActorSpawnParameters SpawnParams;
		SpawnParams.Owner = this;

		RoundManagerInstance = GetWorld()->SpawnActor<ARoundManager>(ARoundManager::StaticClass(), SpawnParams);
		DirectorInstance = GetWorld()->SpawnActor<ADirectorAI>(ADirectorAI::StaticClass(), SpawnParams);

		UE_LOG(LogTemp, Log, TEXT("MimicFacilityGameMode — All systems online."));
	}
}

void AMimicFacilityGameMode::PostLogin(APlayerController* NewPlayer)
{
	Super::PostLogin(NewPlayer);

	if (NewPlayer)
	{
		AMimicFacilityPlayerState* PS = Cast<AMimicFacilityPlayerState>(NewPlayer->PlayerState);
		if (PS)
		{
			PS->SetSubjectNumber(NextSubjectNumber);
			UE_LOG(LogTemp, Log, TEXT("Player logged in — assigned Subject %d"), NextSubjectNumber);
			NextSubjectNumber++;
		}
	}
}
