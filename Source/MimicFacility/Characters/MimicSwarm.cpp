// MimicSwarm.cpp — Swarm Mimic implementation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "MimicSwarm.h"
#include "Components/CapsuleComponent.h"
#include "GameFramework/CharacterMovementComponent.h"

AMimicSwarm::AMimicSwarm()
{
	FlockingRadius = 500.0f;
	SwarmSpeed = 600.0f;

	// Swarm mimics are smaller and faster
	GetCapsuleComponent()->InitCapsuleSize(24.0f, 48.0f);
	GetCharacterMovement()->MaxWalkSpeed = SwarmSpeed;
}

void AMimicSwarm::BeginPlay()
{
	Super::BeginPlay();
	GetCharacterMovement()->MaxWalkSpeed = SwarmSpeed;
	UE_LOG(LogTemp, Log, TEXT("SwarmMimic spawned: %s | Speed: %.0f"), *GetName(), SwarmSpeed);
}

void AMimicSwarm::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);
}
