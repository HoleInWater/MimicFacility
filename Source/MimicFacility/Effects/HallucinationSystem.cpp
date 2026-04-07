// HallucinationSystem.cpp — Spore-driven hallucination system that distorts player perception based on exposure.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "HallucinationSystem.h"
#include "Components/PostProcessComponent.h"
#include "GameFramework/Character.h"
#include "Gear/SporeFilter.h"

UHallucinationSystem::UHallucinationSystem()
{
	PrimaryComponentTick.bCanEverTick = true;
	SporeExposure = 0.0f;
	ExposureDecayRate = 0.05f;
}

void UHallucinationSystem::BeginPlay()
{
	Super::BeginPlay();

	PostProcessComp = NewObject<UPostProcessComponent>(GetOwner(), TEXT("HallucinationPostProcess"));
	if (PostProcessComp)
	{
		PostProcessComp->RegisterComponent();
		PostProcessComp->bUnbound = true;
		PostProcessComp->Priority = 10.0f;
	}
}

void UHallucinationSystem::TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction)
{
	Super::TickComponent(DeltaTime, TickType, ThisTickFunction);

	// Decay exposure when not actively accumulating
	SporeExposure = FMath::Max(0.0f, SporeExposure - ExposureDecayRate * DeltaTime);

	ProcessActiveHallucinations(DeltaTime);
	UpdateSporeThresholds();
	ApplyPostProcessEffects();
}

void UHallucinationSystem::AddSporeExposure(float Amount, float DeltaTime)
{
	float EffectiveAmount = Amount * DeltaTime;

	// Check if owner has an equipped SporeFilter to reduce accumulation
	ACharacter* OwnerCharacter = Cast<ACharacter>(GetOwner());
	if (OwnerCharacter)
	{
		TArray<AActor*> AttachedActors;
		OwnerCharacter->GetAttachedActors(AttachedActors);
		for (AActor* Attached : AttachedActors)
		{
			ASporeFilter* Filter = Cast<ASporeFilter>(Attached);
			if (Filter && Filter->IsWorn())
			{
				EffectiveAmount *= (1.0f - Filter->GetFilterEfficiency());
				break;
			}
		}
	}

	SporeExposure = FMath::Clamp(SporeExposure + EffectiveAmount, 0.0f, 1.0f);
}

void UHallucinationSystem::TriggerHallucination(EHallucinationType Type, float Intensity, float Duration, FVector SourceLocation)
{
	FHallucinationEvent Event;
	Event.Type = Type;
	Event.Intensity = FMath::Clamp(Intensity, 0.0f, 1.0f);
	Event.Duration = Duration;
	Event.SourceLocation = SourceLocation;
	Event.ElapsedTime = 0.0f;

	ActiveHallucinations.Add(Event);

	UE_LOG(LogTemp, Log, TEXT("Hallucination triggered: Type=%d Intensity=%.2f Duration=%.1fs"),
		static_cast<int32>(Type), Intensity, Duration);
}

void UHallucinationSystem::ProcessActiveHallucinations(float DeltaTime)
{
	for (int32 i = ActiveHallucinations.Num() - 1; i >= 0; --i)
	{
		ActiveHallucinations[i].ElapsedTime += DeltaTime;
		if (ActiveHallucinations[i].ElapsedTime >= ActiveHallucinations[i].Duration)
		{
			ActiveHallucinations.RemoveAtSwap(i);
		}
	}
}

void UHallucinationSystem::UpdateSporeThresholds()
{
	FVector OwnerLocation = GetOwner() ? GetOwner()->GetActorLocation() : FVector::ZeroVector;

	if (SporeExposure > 0.9f)
	{
		TriggerHallucination(EHallucinationType::EnvironmentalShift, SporeExposure, 3.0f, OwnerLocation);
	}
	else if (SporeExposure > 0.7f)
	{
		TriggerHallucination(EHallucinationType::ShadowMovement, SporeExposure * 0.8f, 1.5f, OwnerLocation);
		TriggerHallucination(EHallucinationType::FalsePlayerEcho, SporeExposure * 0.6f, 2.0f, OwnerLocation);
	}
	else if (SporeExposure > 0.5f)
	{
		TriggerHallucination(EHallucinationType::VisualFlicker, SporeExposure * 0.5f, 1.0f, OwnerLocation);
	}
	else if (SporeExposure > 0.3f)
	{
		TriggerHallucination(EHallucinationType::AudioDistortion, SporeExposure * 0.3f, 0.8f, OwnerLocation);
	}
}

void UHallucinationSystem::ApplyPostProcessEffects()
{
	if (!PostProcessComp)
	{
		return;
	}

	FPostProcessSettings& Settings = PostProcessComp->Settings;

	// Desaturate as exposure increases
	Settings.bOverride_ColorSaturation = true;
	Settings.ColorSaturation = FVector4(1.0f - SporeExposure * 0.4f, 1.0f - SporeExposure * 0.4f, 1.0f - SporeExposure * 0.4f, 1.0f);

	// Chromatic aberration scales with exposure
	Settings.bOverride_SceneFringeIntensity = true;
	Settings.SceneFringeIntensity = SporeExposure * 5.0f;

	// Vignette darkens peripheral vision
	Settings.bOverride_VignetteIntensity = true;
	Settings.VignetteIntensity = SporeExposure * 0.8f;

	PostProcessComp->BlendWeight = FMath::Clamp(SporeExposure, 0.0f, 1.0f);
}
