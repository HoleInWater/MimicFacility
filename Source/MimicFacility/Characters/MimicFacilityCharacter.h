// MimicFacilityCharacter.h — Player character class with first-person camera, gear slots, and voice chat integration.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Character.h"
#include "MimicFacilityCharacter.generated.h"

class UCameraComponent;
class UInputComponent;

/**
 * AMimicFacilityCharacter
 * The player-controlled character. Handles first-person movement, gear inventory,
 * voice chat input routing, and interaction with facility objects.
 */
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

protected:
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Camera")
	TObjectPtr<UCameraComponent> FirstPersonCamera;
};
