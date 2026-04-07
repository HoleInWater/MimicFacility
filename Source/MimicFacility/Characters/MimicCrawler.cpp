// MimicCrawler.cpp — Ceiling Crawler Mimic implementation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "MimicCrawler.h"
#include "Net/UnrealNetwork.h"

AMimicCrawler::AMimicCrawler()
{
	bIsCeilingMounted = true;
	DropAttackRange = 300.0f;
}

void AMimicCrawler::BeginPlay()
{
	Super::BeginPlay();
	UE_LOG(LogTemp, Log, TEXT("MimicCrawler spawned. CeilingMounted: %s"), bIsCeilingMounted ? TEXT("true") : TEXT("false"));
}

void AMimicCrawler::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);
}

void AMimicCrawler::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);
	DOREPLIFETIME(AMimicCrawler, bIsCeilingMounted);
}
