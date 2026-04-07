// MimicAIController.h — AI Controller for Mimic actors. Handles behavior tree and basic navigation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "AIController.h"
#include "MimicAIController.generated.h"

class UBehaviorTree;
class UNavigationSystemV1;

UCLASS()
class MIMICFACILITY_API AMimicAIController : public AAIController
{
	GENERATED_BODY()

public:
	AMimicAIController();

protected:
	virtual void BeginPlay() override;
	virtual void OnPossess(APawn* InPawn) override;
	virtual void Tick(float DeltaTime) override;

	UPROPERTY(EditDefaultsOnly, BlueprintReadOnly, Category = "AI")
	TObjectPtr<UBehaviorTree> MimicBehaviorTree;

	// Simple patrol fallback when no behavior tree is assigned
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "AI|Patrol")
	float PatrolRadius;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "AI|Patrol")
	float PatrolWaitTime;

private:
	void MoveToRandomPatrolPoint();

	FVector OriginLocation;
	FTimerHandle PatrolTimerHandle;
	bool bIsPatrolling;
};
