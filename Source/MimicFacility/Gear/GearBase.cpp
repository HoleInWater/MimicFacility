// GearBase.cpp — Gear Base implementation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "GearBase.h"
#include "Net/UnrealNetwork.h"

AGearBase::AGearBase()
{
	bReplicates = true;
	bIsConsumable = false;
	UsesRemaining = -1;
}

void AGearBase::BeginPlay()
{
	Super::BeginPlay();
}

void AGearBase::Activate()
{
	// TODO: Override in subclasses. Base implementation handles consumable use tracking.
	if (bIsConsumable && UsesRemaining > 0)
	{
		UsesRemaining--;
	}
}

void AGearBase::OnPickedUp(AActor* NewOwner)
{
	// TODO: Attach to player, update HUD.
}

void AGearBase::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);

	DOREPLIFETIME(AGearBase, UsesRemaining);
}
