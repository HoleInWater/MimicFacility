// FacilityDoor.cpp — Lockable facility door controlled by the Director.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "FacilityDoor.h"
#include "Components/BoxComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Kismet/GameplayStatics.h"
#include "Net/UnrealNetwork.h"

AFacilityDoor::AFacilityDoor()
{
	PrimaryActorTick.bCanEverTick = false;
	bReplicates = true;

	DoorMesh = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("DoorMesh"));
	RootComponent = DoorMesh;

	BlockingVolume = CreateDefaultSubobject<UBoxComponent>(TEXT("BlockingVolume"));
	BlockingVolume->SetupAttachment(RootComponent);
	BlockingVolume->SetCollisionEnabled(ECollisionEnabled::NoCollision);
	BlockingVolume->SetBoxExtent(FVector(50.0f, 100.0f, 120.0f));

	bIsLocked = false;
	bIsOpen = false;
}

void AFacilityDoor::BeginPlay()
{
	Super::BeginPlay();
}

void AFacilityDoor::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);
	DOREPLIFETIME(AFacilityDoor, bIsLocked);
	DOREPLIFETIME(AFacilityDoor, bIsOpen);
}

void AFacilityDoor::Lock()
{
	bIsLocked = true;

	if (bIsOpen)
	{
		ToggleOpen();
	}

	BlockingVolume->SetCollisionEnabled(ECollisionEnabled::QueryAndPhysics);
	BlockingVolume->SetCollisionResponseToAllChannels(ECR_Block);

	UE_LOG(LogTemp, Log, TEXT("FacilityDoor [%s] LOCKED in zone %s"), *GetName(), *ZoneTag.ToString());
}

void AFacilityDoor::Unlock()
{
	bIsLocked = false;
	BlockingVolume->SetCollisionEnabled(ECollisionEnabled::NoCollision);

	UE_LOG(LogTemp, Log, TEXT("FacilityDoor [%s] UNLOCKED in zone %s"), *GetName(), *ZoneTag.ToString());
}

void AFacilityDoor::ToggleOpen()
{
	bIsOpen = !bIsOpen;

	if (bIsOpen)
	{
		DoorMesh->SetRelativeRotation(FRotator(0.0f, 90.0f, 0.0f));
		if (OpenSound)
		{
			UGameplayStatics::PlaySoundAtLocation(this, OpenSound, GetActorLocation());
		}
	}
	else
	{
		DoorMesh->SetRelativeRotation(FRotator::ZeroRotator);
		if (CloseSound)
		{
			UGameplayStatics::PlaySoundAtLocation(this, CloseSound, GetActorLocation());
		}
	}
}

void AFacilityDoor::OnInteract(AActor* Interactor)
{
	if (bIsLocked)
	{
		if (LockedSound)
		{
			UGameplayStatics::PlaySoundAtLocation(this, LockedSound, GetActorLocation());
		}
		return;
	}

	ToggleOpen();
}

void AFacilityDoor::OnRep_IsLocked()
{
	if (bIsLocked)
	{
		BlockingVolume->SetCollisionEnabled(ECollisionEnabled::QueryAndPhysics);
		BlockingVolume->SetCollisionResponseToAllChannels(ECR_Block);
	}
	else
	{
		BlockingVolume->SetCollisionEnabled(ECollisionEnabled::NoCollision);
	}
}
