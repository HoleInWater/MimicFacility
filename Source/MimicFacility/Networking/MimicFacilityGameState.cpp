// MimicFacilityGameState.cpp — Game State implementation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "MimicFacilityGameState.h"
#include "Net/UnrealNetwork.h"

AMimicFacilityGameState::AMimicFacilityGameState()
{
	ActiveMimicCount = 0;
	ContainedMimicCount = 0;
	FalsePositiveCount = 0;
}

void AMimicFacilityGameState::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);

	DOREPLIFETIME(AMimicFacilityGameState, ActiveMimicCount);
	DOREPLIFETIME(AMimicFacilityGameState, ContainedMimicCount);
	DOREPLIFETIME(AMimicFacilityGameState, FalsePositiveCount);
}
