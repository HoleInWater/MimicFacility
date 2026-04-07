// RoundManager.cpp — Round Manager with timer-based advancement and Mimic spawning.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "RoundManager.h"
#include "Characters/MimicBase.h"
#include "Networking/MimicFacilityGameState.h"
#include "NavigationSystem.h"
#include "Net/UnrealNetwork.h"

ARoundManager::ARoundManager()
{
	PrimaryActorTick.bCanEverTick = true;
	bReplicates = true;
	CurrentPhase = ERoundPhase::Round1_Exploration;
	RoundNumber = 1;
	RoundTimer = 0.0f;
	Round1Duration = 120.0f; // 2 minutes for exploration
	RoundDuration = 180.0f;  // 3 minutes per subsequent round
	MimicsPerRound = 2;
}

void ARoundManager::BeginPlay()
{
	Super::BeginPlay();
	UE_LOG(LogTemp, Warning, TEXT("=== ROUND 1: EXPLORATION PHASE ==="));
	UE_LOG(LogTemp, Log, TEXT("Round 1 will last %.0f seconds."), Round1Duration);
}

void ARoundManager::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	if (!HasAuthority() || CurrentPhase == ERoundPhase::GameOver)
		return;

	RoundTimer += DeltaTime;

	float Duration = (RoundNumber == 1) ? Round1Duration : RoundDuration;

	if (RoundTimer >= Duration)
	{
		AdvanceRound();
	}
}

void ARoundManager::AdvanceRound()
{
	if (!HasAuthority()) return;

	RoundTimer = 0.0f;
	RoundNumber++;

	ERoundPhase OldPhase = CurrentPhase;

	if (RoundNumber == 2)
	{
		CurrentPhase = ERoundPhase::Round2_Infiltration;
		UE_LOG(LogTemp, Warning, TEXT("=== ROUND 2: INFILTRATION PHASE ==="));
	}
	else if (RoundNumber >= 3)
	{
		CurrentPhase = ERoundPhase::Round3_Escalation;
		UE_LOG(LogTemp, Warning, TEXT("=== ROUND %d: ESCALATION PHASE ==="), RoundNumber);
	}

	// Update game state
	AMimicFacilityGameState* GS = Cast<AMimicFacilityGameState>(GetWorld()->GetGameState());
	if (GS)
	{
		GS->SetCurrentRound(RoundNumber);
	}

	// Spawn mimics for this round
	if (RoundNumber >= 2)
	{
		SpawnMimicsForRound();
	}

	OnRoundChanged.Broadcast(RoundNumber, CurrentPhase);
}

void ARoundManager::SpawnMimicsForRound()
{
	if (!HasAuthority()) return;

	UNavigationSystemV1* NavSys = UNavigationSystemV1::GetCurrent(GetWorld());
	if (!NavSys) return;

	AMimicFacilityGameState* GS = Cast<AMimicFacilityGameState>(GetWorld()->GetGameState());

	int32 SpawnCount = MimicsPerRound + (RoundNumber - 2); // Escalating count

	for (int32 i = 0; i < SpawnCount; i++)
	{
		FNavLocation SpawnPoint;
		if (NavSys->GetRandomReachablePointInRadius(GetActorLocation(), 3000.0f, SpawnPoint))
		{
			FActorSpawnParameters SpawnParams;
			AMimicBase* Mimic = GetWorld()->SpawnActor<AMimicBase>(
				AMimicBase::StaticClass(),
				SpawnPoint.Location + FVector(0, 0, 100.0f),
				FRotator::ZeroRotator,
				SpawnParams
			);

			if (Mimic && GS)
			{
				GS->AddActiveMimic();
				UE_LOG(LogTemp, Log, TEXT("Spawned Mimic at %s"), *SpawnPoint.Location.ToString());
			}
		}
	}

	UE_LOG(LogTemp, Warning, TEXT("Round %d — Spawned %d Mimics."), RoundNumber, SpawnCount);
}

void ARoundManager::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);

	DOREPLIFETIME(ARoundManager, CurrentPhase);
	DOREPLIFETIME(ARoundManager, RoundNumber);
}
