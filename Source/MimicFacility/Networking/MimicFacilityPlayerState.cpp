// MimicFacilityPlayerState.cpp — Player State implementation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "MimicFacilityPlayerState.h"
#include "Net/UnrealNetwork.h"

AMimicFacilityPlayerState::AMimicFacilityPlayerState()
{
	SubjectNumber = 0;
	bIsConverted = false;
}

void AMimicFacilityPlayerState::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);

	DOREPLIFETIME(AMimicFacilityPlayerState, SubjectNumber);
	DOREPLIFETIME(AMimicFacilityPlayerState, bIsConverted);
}
