// MimicFacilityCharacter.cpp — Player character implementation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "MimicFacilityCharacter.h"
#include "Camera/CameraComponent.h"
#include "Components/CapsuleComponent.h"
#include "Components/InputComponent.h"

AMimicFacilityCharacter::AMimicFacilityCharacter()
{
	PrimaryActorTick.bCanEverTick = true;

	FirstPersonCamera = CreateDefaultSubobject<UCameraComponent>(TEXT("FirstPersonCamera"));
	FirstPersonCamera->SetupAttachment(GetCapsuleComponent());
	FirstPersonCamera->SetRelativeLocation(FVector(0.0f, 0.0f, 64.0f));
	FirstPersonCamera->bUsePawnControlRotation = true;
}

void AMimicFacilityCharacter::BeginPlay()
{
	Super::BeginPlay();
}

void AMimicFacilityCharacter::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);
}

void AMimicFacilityCharacter::SetupPlayerInputComponent(UInputComponent* PlayerInputComponent)
{
	Super::SetupPlayerInputComponent(PlayerInputComponent);
}
