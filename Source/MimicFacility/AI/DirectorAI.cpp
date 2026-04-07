// DirectorAI.cpp — The Director AI implementation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "DirectorAI.h"

ADirectorAI::ADirectorAI()
{
	PrimaryActorTick.bCanEverTick = true;
	CurrentState = EDirectorState::Observing;
}

void ADirectorAI::BeginPlay()
{
	Super::BeginPlay();

	// Evaluate game state every 5 seconds.
	GetWorldTimerManager().SetTimer(
		StateEvaluationTimer,
		this,
		&ADirectorAI::EvaluateGameState,
		5.0f,
		true
	);
}

void ADirectorAI::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);
}

void ADirectorAI::EvaluateGameState()
{
	// TODO: Poll MimicFacilityGameState for player count, mimic count, round number.
	// TODO: Evaluate state transition conditions and update CurrentState.
	// TODO: Generate Director dialogue via Claude API or fallback pool.
}
