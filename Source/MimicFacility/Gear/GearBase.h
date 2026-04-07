// GearBase.h — Base class for all player gear items. Handles pickup, equip, and use interface.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "GearBase.generated.h"

class USphereComponent;
class UStaticMeshComponent;

UCLASS(Abstract)
class MIMICFACILITY_API AGearBase : public AActor
{
	GENERATED_BODY()

public:
	AGearBase();

protected:
	virtual void BeginPlay() override;

public:
	UFUNCTION(BlueprintCallable, Category = "Gear")
	virtual void Activate();

	UFUNCTION(BlueprintCallable, Category = "Gear")
	virtual void OnPickedUp(AActor* NewOwner);

	UFUNCTION(BlueprintPure, Category = "Gear")
	bool IsPickedUp() const { return bIsPickedUp; }

	UFUNCTION(BlueprintPure, Category = "Gear")
	FText GetGearName() const { return GearName; }

protected:
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Gear")
	TObjectPtr<UStaticMeshComponent> GearMesh;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Gear")
	TObjectPtr<USphereComponent> PickupCollision;

	UPROPERTY(EditDefaultsOnly, BlueprintReadOnly, Category = "Gear")
	FText GearName;

	UPROPERTY(EditDefaultsOnly, BlueprintReadOnly, Category = "Gear")
	bool bIsConsumable;

	UPROPERTY(Replicated, BlueprintReadOnly, Category = "Gear")
	int32 UsesRemaining;

	UPROPERTY(Replicated, BlueprintReadOnly, Category = "Gear")
	bool bIsPickedUp;

	virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;
};
