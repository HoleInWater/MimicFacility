// MimicSwarm.h — Swarm Mimic variant. Small, fast, uses flocking behavior. Spawns from Trigger Word events.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "MimicBase.h"
#include "MimicSwarm.generated.h"

/**
 * AMimicSwarm
 * Small, partially-formed Mimic that uses flocking/horde behavior.
 * Low individual threat, overwhelming in numbers. Spawns exclusively from Trigger Word activation.
 */
UCLASS()
class MIMICFACILITY_API AMimicSwarm : public AMimicBase
{
	GENERATED_BODY()

public:
	AMimicSwarm();

protected:
	virtual void BeginPlay() override;

public:
	virtual void Tick(float DeltaTime) override;

protected:
	/** Reference to other swarm members for flocking calculations. */
	UPROPERTY(BlueprintReadOnly, Category = "Mimic|Swarm")
	TArray<TObjectPtr<AMimicSwarm>> FlockMembers;
};
