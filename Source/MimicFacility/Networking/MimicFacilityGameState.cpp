// MimicFacilityGameState.cpp — Game State implementation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "MimicFacilityGameState.h"
#include "Net/UnrealNetwork.h"

AMimicFacilityGameState::AMimicFacilityGameState()
{
	ActiveMimicCount = 0;
	ContainedMimicCount = 0;
	FalsePositiveCount = 0;
	CurrentRound = 1;
}

void AMimicFacilityGameState::AddActiveMimic()
{
	if (HasAuthority())
	{
		ActiveMimicCount++;
		UE_LOG(LogTemp, Log, TEXT("GameState — Active Mimics: %d"), ActiveMimicCount);
	}
}

void AMimicFacilityGameState::RemoveActiveMimic()
{
	if (HasAuthority())
	{
		ActiveMimicCount = FMath::Max(0, ActiveMimicCount - 1);
		UE_LOG(LogTemp, Log, TEXT("GameState — Active Mimics: %d"), ActiveMimicCount);
	}
}

void AMimicFacilityGameState::IncrementContained()
{
	if (HasAuthority())
	{
		ContainedMimicCount++;
		RemoveActiveMimic();
		UE_LOG(LogTemp, Log, TEXT("GameState — Contained Mimics: %d"), ContainedMimicCount);
	}
}

void AMimicFacilityGameState::IncrementFalsePositive()
{
	if (HasAuthority())
	{
		FalsePositiveCount++;
		UE_LOG(LogTemp, Warning, TEXT("GameState — False Positives: %d"), FalsePositiveCount);
	}
}

void AMimicFacilityGameState::SetCurrentRound(int32 Round)
{
	if (HasAuthority())
	{
		CurrentRound = Round;
		UE_LOG(LogTemp, Log, TEXT("GameState — Round: %d"), CurrentRound);
	}
}

void AMimicFacilityGameState::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);

	DOREPLIFETIME(AMimicFacilityGameState, ActiveMimicCount);
	DOREPLIFETIME(AMimicFacilityGameState, ContainedMimicCount);
	DOREPLIFETIME(AMimicFacilityGameState, FalsePositiveCount);
	DOREPLIFETIME(AMimicFacilityGameState, CurrentRound);
}
