// MimicFacilityPlayerState.cpp — Player State with subject number assignment and conversion tracking.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "MimicFacilityPlayerState.h"
#include "Net/UnrealNetwork.h"

AMimicFacilityPlayerState::AMimicFacilityPlayerState()
{
	SubjectNumber = 0;
	bIsConverted = false;
}

void AMimicFacilityPlayerState::BeginPlay()
{
	Super::BeginPlay();
	UE_LOG(LogTemp, Log, TEXT("PlayerState initialized — Subject %d"), SubjectNumber);
}

void AMimicFacilityPlayerState::SetSubjectNumber(int32 Number)
{
	if (HasAuthority())
	{
		SubjectNumber = Number;
		UE_LOG(LogTemp, Log, TEXT("Player assigned Subject Number: %d"), SubjectNumber);
	}
}

void AMimicFacilityPlayerState::MarkConverted()
{
	if (HasAuthority())
	{
		bIsConverted = true;
		UE_LOG(LogTemp, Warning, TEXT("Subject %d has been CONVERTED!"), SubjectNumber);
	}
}

void AMimicFacilityPlayerState::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);

	DOREPLIFETIME(AMimicFacilityPlayerState, SubjectNumber);
	DOREPLIFETIME(AMimicFacilityPlayerState, bIsConverted);
}
