// AudioScanner.cpp — Audio Scanner gear: waveform analysis for mimic detection.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "AudioScanner.h"
#include "Characters/MimicBase.h"
#include "Kismet/GameplayStatics.h"

AAudioScanner::AAudioScanner()
{
	GearName = FText::FromString(TEXT("Audio Scanner"));
	bIsConsumable = false;
	ScanRange = 500.0f;
	ScanDuration = 2.0f;
	bIsScanning = false;
}

void AAudioScanner::BeginPlay()
{
	Super::BeginPlay();
}

void AAudioScanner::Activate()
{
	if (bIsScanning) return;

	bIsScanning = true;
	UE_LOG(LogTemp, Log, TEXT("AudioScanner — Scanning... (%.1fs)"), ScanDuration);

	GetWorldTimerManager().SetTimer(ScanTimer, this, &AAudioScanner::CompleteScan, ScanDuration, false);
}

void AAudioScanner::CompleteScan()
{
	bIsScanning = false;
	PerformScan();
}

void AAudioScanner::PerformScan()
{
	LastResult = FScanResult();
	LastResult.ScanTimestamp = GetWorld()->GetTimeSeconds();

	AActor* OwnerActor = GetOwner();
	if (!OwnerActor) return;

	FVector ScanOrigin = OwnerActor->GetActorLocation();

	// Find nearest character-type actor within range
	TArray<AActor*> NearbyActors;
	UGameplayStatics::GetAllActorsOfClass(GetWorld(), ACharacter::StaticClass(), NearbyActors);

	AActor* NearestTarget = nullptr;
	float NearestDist = ScanRange;

	for (AActor* Actor : NearbyActors)
	{
		if (Actor == OwnerActor) continue;

		float Dist = FVector::Dist(ScanOrigin, Actor->GetActorLocation());
		if (Dist < NearestDist)
		{
			NearestDist = Dist;
			NearestTarget = Actor;
		}
	}

	if (!NearestTarget)
	{
		UE_LOG(LogTemp, Log, TEXT("AudioScanner — No targets in range"));
		return;
	}

	LastResult.TargetID = NearestTarget->GetName();

	// Check if target is a mimic
	AMimicBase* Mimic = Cast<AMimicBase>(NearestTarget);
	if (Mimic)
	{
		LastResult.bIsMimic = true;
		// Waveform integrity: mimics show micro-repetitions (0.6-0.85 integrity)
		LastResult.WaveformIntegrity = FMath::FRandRange(0.6f, 0.85f);
		UE_LOG(LogTemp, Warning, TEXT("AudioScanner — ANOMALY DETECTED: %s (integrity: %.2f)"),
			*LastResult.TargetID, LastResult.WaveformIntegrity);
	}
	else
	{
		LastResult.bIsMimic = false;
		// Real players show organic, variable waveforms (0.92-1.0 integrity)
		LastResult.WaveformIntegrity = FMath::FRandRange(0.92f, 1.0f);
		UE_LOG(LogTemp, Log, TEXT("AudioScanner — Clean reading: %s (integrity: %.2f)"),
			*LastResult.TargetID, LastResult.WaveformIntegrity);
	}

	OnScanComplete.Broadcast(LastResult);
}
