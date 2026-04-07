// MimicAIController.h — AI Controller for Mimic actors. Runs the Mimic behavior tree and manages blackboard data.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "AIController.h"
#include "MimicAIController.generated.h"

class UBehaviorTreeComponent;
class UBlackboardComponent;

/**
 * AMimicAIController
 * Controls Mimic actors using Unreal's Behavior Tree and Blackboard systems.
 * Handles target selection, state transitions, and voice playback triggers.
 */
UCLASS()
class MIMICFACILITY_API AMimicAIController : public AAIController
{
	GENERATED_BODY()

public:
	AMimicAIController();

protected:
	virtual void BeginPlay() override;
	virtual void OnPossess(APawn* InPawn) override;

protected:
	/** Behavior tree asset to run for this Mimic. */
	UPROPERTY(EditDefaultsOnly, BlueprintReadOnly, Category = "AI")
	TObjectPtr<UBehaviorTree> MimicBehaviorTree;
};
