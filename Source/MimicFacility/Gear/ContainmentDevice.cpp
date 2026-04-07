// ContainmentDevice.cpp — Containment device: single-use mimic capture.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "ContainmentDevice.h"
#include "Characters/MimicBase.h"
#include "Characters/MimicFacilityCharacter.h"
#include "Kismet/GameplayStatics.h"

AContainmentDevice::AContainmentDevice()
{
	GearName = FText::FromString(TEXT("Containment Device"));
	bIsConsumable = true;
	UsesRemaining = 1;
	ContainmentRadius = 300.0f;
	StunDuration = 10.0f;
}

void AContainmentDevice::Activate()
{
	if (UsesRemaining <= 0) return;

	AActor* OwnerActor = GetOwner();
	if (!OwnerActor) return;

	FVector Origin = OwnerActor->GetActorLocation();

	// Find nearest actor in containment radius
	TArray<AActor*> NearbyActors;
	UGameplayStatics::GetAllActorsOfClass(GetWorld(), ACharacter::StaticClass(), NearbyActors);

	AActor* NearestTarget = nullptr;
	float NearestDist = ContainmentRadius;

	for (AActor* Actor : NearbyActors)
	{
		if (Actor == OwnerActor) continue;

		float Dist = FVector::Dist(Origin, Actor->GetActorLocation());
		if (Dist < NearestDist)
		{
			NearestDist = Dist;
			NearestTarget = Actor;
		}
	}

	// Consume the device regardless of outcome
	Super::Activate();

	if (!NearestTarget)
	{
		UE_LOG(LogTemp, Log, TEXT("ContainmentDevice — No target in range. Device wasted."));
		return;
	}

	AMimicBase* Mimic = Cast<AMimicBase>(NearestTarget);
	if (Mimic)
	{
		// Successful containment
		UE_LOG(LogTemp, Warning, TEXT("ContainmentDevice — MIMIC CONTAINED: %s"), *Mimic->GetName());
		Mimic->Destroy();
	}
	else
	{
		// False positive — hit a real player
		UE_LOG(LogTemp, Error, TEXT("ContainmentDevice — FALSE POSITIVE: %s is a real player!"), *NearestTarget->GetName());
		// Stun the real player (disable input for StunDuration)
		AMimicFacilityCharacter* Player = Cast<AMimicFacilityCharacter>(NearestTarget);
		if (Player)
		{
			APlayerController* PC = Cast<APlayerController>(Player->GetController());
			if (PC)
			{
				PC->DisableInput(PC);
				FTimerHandle StunHandle;
				FTimerDelegate StunDelegate;
				StunDelegate.BindLambda([PC]()
				{
					if (PC) PC->EnableInput(PC);
				});
				GetWorldTimerManager().SetTimer(StunHandle, StunDelegate, StunDuration, false);
			}
		}
	}
}
