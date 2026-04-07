// MimicAIController.cpp — Mimic AI Controller with behavior tree and patrol fallback.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "MimicAIController.h"
#include "BehaviorTree/BehaviorTree.h"
#include "BehaviorTree/BlackboardComponent.h"
#include "NavigationSystem.h"
#include "Navigation/PathFollowingComponent.h"

AMimicAIController::AMimicAIController()
{
	PrimaryActorTick.bCanEverTick = true;
	PatrolRadius = 1500.0f;
	PatrolWaitTime = 3.0f;
	bIsPatrolling = false;
}

void AMimicAIController::BeginPlay()
{
	Super::BeginPlay();
}

void AMimicAIController::OnPossess(APawn* InPawn)
{
	Super::OnPossess(InPawn);

	OriginLocation = InPawn->GetActorLocation();

	if (MimicBehaviorTree)
	{
		RunBehaviorTree(MimicBehaviorTree);
		UE_LOG(LogTemp, Log, TEXT("MimicAIController — Running behavior tree on %s"), *InPawn->GetName());
	}
	else
	{
		// No behavior tree assigned — use simple patrol fallback for testing
		UE_LOG(LogTemp, Log, TEXT("MimicAIController — No behavior tree. Using patrol fallback on %s"), *InPawn->GetName());
		MoveToRandomPatrolPoint();
	}
}

void AMimicAIController::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	// If using patrol fallback and not currently moving, pick a new point
	if (!MimicBehaviorTree && !bIsPatrolling && GetPawn())
	{
		if (GetPathFollowingComponent() && GetPathFollowingComponent()->GetStatus() == EPathFollowingStatus::Idle)
		{
			// Wait, then patrol again
			if (!PatrolTimerHandle.IsValid())
			{
				GetWorldTimerManager().SetTimer(
					PatrolTimerHandle,
					this,
					&AMimicAIController::MoveToRandomPatrolPoint,
					PatrolWaitTime,
					false
				);
			}
		}
	}
}

void AMimicAIController::MoveToRandomPatrolPoint()
{
	PatrolTimerHandle.Invalidate();

	UNavigationSystemV1* NavSys = UNavigationSystemV1::GetCurrent(GetWorld());
	if (!NavSys || !GetPawn()) return;

	FNavLocation RandomPoint;
	if (NavSys->GetRandomReachablePointInRadius(OriginLocation, PatrolRadius, RandomPoint))
	{
		MoveToLocation(RandomPoint.Location);
		UE_LOG(LogTemp, Verbose, TEXT("Mimic patrolling to: %s"), *RandomPoint.Location.ToString());
	}
}
