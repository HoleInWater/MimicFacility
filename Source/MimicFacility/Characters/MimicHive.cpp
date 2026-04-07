// MimicHive.cpp — Hive Mimic: area denial through expanding organic mass with distorted multi-voice audio.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "MimicHive.h"
#include "Components/AudioComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Components/SphereComponent.h"

AMimicHive::AMimicHive()
{
	PrimaryActorTick.bCanEverTick = true;
	bReplicates = true;

	DenialZone = CreateDefaultSubobject<USphereComponent>(TEXT("DenialZone"));
	DenialZone->SetSphereRadius(200.0f);
	DenialZone->SetCollisionProfileName(TEXT("OverlapAllDynamic"));
	SetRootComponent(DenialZone);

	HiveMesh = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("HiveMesh"));
	HiveMesh->SetupAttachment(DenialZone);
	HiveMesh->SetCollisionEnabled(ECollisionEnabled::QueryAndPhysics);
	HiveMesh->SetCollisionResponseToAllChannels(ECR_Block);

	HiveAudio = CreateDefaultSubobject<UAudioComponent>(TEXT("HiveAudio"));
	HiveAudio->SetupAttachment(DenialZone);
	HiveAudio->bAutoActivate = false;

	GrowthRate = 15.0f;
	MaxRadius = 500.0f;
	DamagePerSecond = 10.0f;
	CurrentRadius = 200.0f;
	GrowthDirection = FVector::ZeroVector;
}

void AMimicHive::BeginPlay()
{
	Super::BeginPlay();

	// Play distorted multi-voice audio periodically
	GetWorldTimerManager().SetTimer(VoiceTimer, this, &AMimicHive::PlayMultiVoice, 5.0f, true);

	UE_LOG(LogTemp, Warning, TEXT("HiveMimic spawned at %s — area denial active"), *GetActorLocation().ToString());
}

void AMimicHive::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	if (HasAuthority())
	{
		GrowHive(DeltaTime);
	}
}

void AMimicHive::AddVoiceProfile(const FString& PlayerID)
{
	AbsorbedVoiceProfiles.AddUnique(PlayerID);
	UE_LOG(LogTemp, Log, TEXT("HiveMimic absorbed voice profile: %s (total: %d)"),
		*PlayerID, AbsorbedVoiceProfiles.Num());
}

void AMimicHive::SetGrowthTarget(const FVector& Direction)
{
	GrowthDirection = Direction.GetSafeNormal();
}

void AMimicHive::GrowHive(float DeltaTime)
{
	if (CurrentRadius >= MaxRadius) return;

	CurrentRadius += GrowthRate * DeltaTime;
	CurrentRadius = FMath::Min(CurrentRadius, MaxRadius);

	DenialZone->SetSphereRadius(CurrentRadius);
	HiveMesh->SetWorldScale3D(FVector(CurrentRadius / 200.0f));

	// Slowly move in growth direction
	if (!GrowthDirection.IsZero())
	{
		FVector NewLocation = GetActorLocation() + GrowthDirection * GrowthRate * 0.5f * DeltaTime;
		SetActorLocation(NewLocation);
	}
}

void AMimicHive::PlayMultiVoice()
{
	if (AbsorbedVoiceProfiles.Num() == 0) return;

	// In full implementation: layer multiple TTS outputs simultaneously in distorted unison
	// For now: log the effect
	UE_LOG(LogTemp, Warning, TEXT("[HIVE] Speaking with %d voices simultaneously"), AbsorbedVoiceProfiles.Num());
}
