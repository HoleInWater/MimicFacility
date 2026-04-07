// MimicFacilityCharacter.h — Player character with first-person camera, movement, gear slots, and voice chat integration.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Character.h"
#include "MimicFacilityCharacter.generated.h"

class UCameraComponent;
class AGearBase;

UCLASS(config=Game)
class MIMICFACILITY_API AMimicFacilityCharacter : public ACharacter
{
	GENERATED_BODY()

public:
	AMimicFacilityCharacter();

protected:
	virtual void BeginPlay() override;

public:
	virtual void Tick(float DeltaTime) override;
	virtual void SetupPlayerInputComponent(UInputComponent* PlayerInputComponent) override;

	UFUNCTION(BlueprintCallable, Category = "Gear")
	void EquipGear(AGearBase* Gear);

	UFUNCTION(BlueprintCallable, Category = "Gear")
	AGearBase* GetEquippedGear() const { return EquippedGear; }

protected:
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Camera")
	TObjectPtr<UCameraComponent> FirstPersonCamera;

	UPROPERTY(BlueprintReadOnly, Category = "Gear")
	TObjectPtr<AGearBase> EquippedGear;

	// Movement input handlers
	void MoveForward(float Value);
	void MoveRight(float Value);
	void LookUpRate(float Value);
	void LookRightRate(float Value);

	// Action input handlers
	void OnInteract();
	void OnUseGear();
	void OnToggleFlashlight();

	UPROPERTY(EditDefaultsOnly, Category = "Movement")
	float BaseLookUpRate;

	UPROPERTY(EditDefaultsOnly, Category = "Movement")
	float BaseTurnRate;
};
