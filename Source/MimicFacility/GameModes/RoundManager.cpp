// RoundManager.cpp — Round Manager implementation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "RoundManager.h"
#include "Net/UnrealNetwork.h"

ARoundManager::ARoundManager()
{
	PrimaryActorTick.bCanEverTick = true;
	bReplicates = true;
	CurrentPhase = ERoundPhase::Round1_Exploration;
	RoundNumber = 1;
}

void ARoundManager::BeginPlay()
{
	Super::BeginPlay();
}

void ARoundManager::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);
}

void ARoundManager::AdvanceRound()
{
	// TODO: Transition to next phase, trigger Mimic spawns, notify Director AI.
	RoundNumber++;
}

void ARoundManager::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);

	DOREPLIFETIME(ARoundManager, CurrentPhase);
	DOREPLIFETIME(ARoundManager, RoundNumber);
}
