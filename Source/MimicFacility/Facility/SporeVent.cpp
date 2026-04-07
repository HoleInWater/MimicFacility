// SporeVent.cpp — Spore emitter controlled by the Director. Damages overlapping characters.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "SporeVent.h"
#include "Components/SphereComponent.h"
#include "NiagaraComponent.h"
#include "GameFramework/Character.h"
#include "Kismet/GameplayStatics.h"
#include "Net/UnrealNetwork.h"

ASporeVent::ASporeVent()
{
	PrimaryActorTick.bCanEverTick = false;
	bReplicates = true;

	EffectRadius = CreateDefaultSubobject<USphereComponent>(TEXT("EffectRadius"));
	RootComponent = EffectRadius;
	EffectRadius->SetSphereRadius(500.0f);
	EffectRadius->SetCollisionEnabled(ECollisionEnabled::QueryOnly);
	EffectRadius->SetCollisionResponseToAllChannels(ECR_Overlap);
	EffectRadius->SetGenerateOverlapEvents(true);

	SporeVFX = CreateDefaultSubobject<UNiagaraComponent>(TEXT("SporeVFX"));
	SporeVFX->SetupAttachment(RootComponent);
	SporeVFX->SetAutoActivate(false);

	bIsActive = false;
	SporeRadius = 500.0f;
	SporeDamageRate = 5.0f;
}

void ASporeVent::BeginPlay()
{
	Super::BeginPlay();

	EffectRadius->SetSphereRadius(SporeRadius);
	EffectRadius->OnComponentBeginOverlap.AddDynamic(this, &ASporeVent::OnOverlapBegin);
}

void ASporeVent::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);
	DOREPLIFETIME(ASporeVent, bIsActive);
}

void ASporeVent::Activate()
{
	bIsActive = true;
	SporeVFX->Activate();

	if (HasAuthority())
	{
		GetWorldTimerManager().SetTimer(
			DamageTimerHandle,
			this,
			&ASporeVent::ApplySporeDamage,
			1.0f,
			true
		);
	}

	UE_LOG(LogTemp, Log, TEXT("SporeVent [%s] ACTIVATED in zone %s"), *GetName(), *ZoneTag.ToString());
}

void ASporeVent::Deactivate()
{
	bIsActive = false;
	SporeVFX->Deactivate();
	GetWorldTimerManager().ClearTimer(DamageTimerHandle);

	UE_LOG(LogTemp, Log, TEXT("SporeVent [%s] DEACTIVATED in zone %s"), *GetName(), *ZoneTag.ToString());
}

void ASporeVent::OnOverlapBegin(UPrimitiveComponent* OverlappedComp, AActor* OtherActor, UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep, const FHitResult& SweepResult)
{
	if (!bIsActive || !HasAuthority()) return;

	ACharacter* Character = Cast<ACharacter>(OtherActor);
	if (Character)
	{
		UGameplayStatics::ApplyDamage(Character, SporeDamageRate, nullptr, this, nullptr);
	}
}

void ASporeVent::ApplySporeDamage()
{
	if (!bIsActive) return;

	TArray<AActor*> OverlappingActors;
	EffectRadius->GetOverlappingActors(OverlappingActors, ACharacter::StaticClass());

	for (AActor* Actor : OverlappingActors)
	{
		UGameplayStatics::ApplyDamage(Actor, SporeDamageRate, nullptr, this, nullptr);
	}
}

void ASporeVent::OnRep_IsActive()
{
	if (bIsActive)
	{
		SporeVFX->Activate();
	}
	else
	{
		SporeVFX->Deactivate();
	}
}
