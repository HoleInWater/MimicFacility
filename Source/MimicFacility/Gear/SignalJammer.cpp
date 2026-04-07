// SignalJammer.cpp — Signal Jammer: blocks Director intercoms in a radius while battery lasts.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "SignalJammer.h"
#include "Components/SphereComponent.h"

ASignalJammer::ASignalJammer()
{
	PrimaryActorTick.bCanEverTick = true;
	GearName = FText::FromString(TEXT("Signal Jammer"));
	bIsConsumable = false;
	JamRadius = 800.0f;
	MaxBatteryLife = 60.0f;
	BatteryLife = MaxBatteryLife;
	bIsActive = false;

	JamZone = CreateDefaultSubobject<USphereComponent>(TEXT("JamZone"));
	JamZone->SetupAttachment(RootComponent);
	JamZone->SetSphereRadius(JamRadius);
	JamZone->SetCollisionEnabled(ECollisionEnabled::QueryOnly);
	JamZone->SetCollisionResponseToAllChannels(ECR_Overlap);
	JamZone->SetVisibility(false);
}

void ASignalJammer::Activate()
{
	if (BatteryLife <= 0.0f) return;

	bIsActive = !bIsActive;
	UE_LOG(LogTemp, Log, TEXT("SignalJammer %s (battery: %.1fs)"),
		bIsActive ? TEXT("ACTIVATED") : TEXT("DEACTIVATED"), BatteryLife);
}

void ASignalJammer::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	if (bIsActive)
	{
		BatteryLife -= DeltaTime;
		if (BatteryLife <= 0.0f)
		{
			BatteryLife = 0.0f;
			bIsActive = false;
			UE_LOG(LogTemp, Warning, TEXT("SignalJammer — Battery depleted."));
		}
	}
}
