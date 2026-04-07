// MimicBase.cpp — Base Mimic implementation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "MimicBase.h"
#include "Net/UnrealNetwork.h"

AMimicBase::AMimicBase()
{
	PrimaryActorTick.bCanEverTick = true;
	bReplicates = true;
	CurrentState = EMimicState::Infiltrating;
	bIsIdentified = false;
}

void AMimicBase::BeginPlay()
{
	Super::BeginPlay();
}

void AMimicBase::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);
}

void AMimicBase::OnRep_MimicSkin()
{
	// TODO: Apply the duplicated player skin mesh and materials when VoiceProfileID replicates.
}

void AMimicBase::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);

	DOREPLIFETIME(AMimicBase, VoiceProfileID);
	DOREPLIFETIME(AMimicBase, CurrentState);
	DOREPLIFETIME(AMimicBase, bIsIdentified);
}
