// MimicAIController.cpp — Mimic AI Controller implementation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "MimicAIController.h"
#include "BehaviorTree/BehaviorTree.h"
#include "BehaviorTree/BlackboardComponent.h"

AMimicAIController::AMimicAIController()
{
}

void AMimicAIController::BeginPlay()
{
	Super::BeginPlay();
}

void AMimicAIController::OnPossess(APawn* InPawn)
{
	Super::OnPossess(InPawn);

	if (MimicBehaviorTree)
	{
		RunBehaviorTree(MimicBehaviorTree);
	}
}
