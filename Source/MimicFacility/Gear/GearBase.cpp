// GearBase.cpp — Gear Base implementation with visible mesh and pickup collision.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "GearBase.h"
#include "Components/SphereComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Net/UnrealNetwork.h"

AGearBase::AGearBase()
{
	bReplicates = true;
	bIsConsumable = false;
	bIsPickedUp = false;
	UsesRemaining = -1;

	PickupCollision = CreateDefaultSubobject<USphereComponent>(TEXT("PickupCollision"));
	PickupCollision->SetSphereRadius(80.0f);
	PickupCollision->SetCollisionProfileName(TEXT("OverlapAllDynamic"));
	SetRootComponent(PickupCollision);

	GearMesh = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("GearMesh"));
	GearMesh->SetupAttachment(PickupCollision);
	GearMesh->SetCollisionEnabled(ECollisionEnabled::NoCollision);

	// Default cube mesh will be set in Blueprint or replaced — this gives it a visible shape for testing
	static ConstructorHelpers::FObjectFinder<UStaticMesh> CubeMesh(TEXT("/Engine/BasicShapes/Cube.Cube"));
	if (CubeMesh.Succeeded())
	{
		GearMesh->SetStaticMesh(CubeMesh.Object);
		GearMesh->SetWorldScale3D(FVector(0.2f, 0.2f, 0.2f));
	}
}

void AGearBase::BeginPlay()
{
	Super::BeginPlay();
	UE_LOG(LogTemp, Log, TEXT("Gear spawned: %s"), *GearName.ToString());
}

void AGearBase::Activate()
{
	if (bIsConsumable && UsesRemaining > 0)
	{
		UsesRemaining--;
		UE_LOG(LogTemp, Log, TEXT("Gear %s activated. Uses remaining: %d"), *GearName.ToString(), UsesRemaining);

		if (UsesRemaining <= 0)
		{
			UE_LOG(LogTemp, Log, TEXT("Gear %s depleted."), *GearName.ToString());
		}
	}
}

void AGearBase::OnPickedUp(AActor* NewOwner)
{
	if (!NewOwner) return;

	bIsPickedUp = true;
	SetOwner(NewOwner);
	AttachToActor(NewOwner, FAttachmentTransformRules::SnapToTargetNotIncludingScale);

	// Hide the world mesh — gear is now "in inventory"
	GearMesh->SetVisibility(false);
	PickupCollision->SetCollisionEnabled(ECollisionEnabled::NoCollision);

	UE_LOG(LogTemp, Log, TEXT("Gear %s picked up by %s"), *GearName.ToString(), *NewOwner->GetName());
}

void AGearBase::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);

	DOREPLIFETIME(AGearBase, UsesRemaining);
	DOREPLIFETIME(AGearBase, bIsPickedUp);
}
