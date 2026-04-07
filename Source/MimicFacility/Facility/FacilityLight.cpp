// FacilityLight.cpp — Director-controllable light with flicker support.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "FacilityLight.h"
#include "Components/PointLightComponent.h"
#include "Net/UnrealNetwork.h"

AFacilityLight::AFacilityLight()
{
	PrimaryActorTick.bCanEverTick = false;
	bReplicates = true;

	LightComponent = CreateDefaultSubobject<UPointLightComponent>(TEXT("LightComponent"));
	RootComponent = LightComponent;

	DefaultIntensity = 5000.0f;
	bIsOn = true;
	bWasOnBeforeFlicker = true;
}

void AFacilityLight::BeginPlay()
{
	Super::BeginPlay();
	LightComponent->SetIntensity(bIsOn ? DefaultIntensity : 0.0f);
}

void AFacilityLight::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);
	DOREPLIFETIME(AFacilityLight, bIsOn);
}

void AFacilityLight::TurnOff()
{
	bIsOn = false;
	LightComponent->SetIntensity(0.0f);
}

void AFacilityLight::TurnOn()
{
	bIsOn = true;
	LightComponent->SetIntensity(DefaultIntensity);
}

void AFacilityLight::Flicker(float Duration)
{
	bWasOnBeforeFlicker = bIsOn;

	GetWorldTimerManager().SetTimer(
		FlickerTimerHandle,
		this,
		&AFacilityLight::FlickerTick,
		0.1f,
		true
	);

	GetWorldTimerManager().SetTimer(
		FlickerEndHandle,
		this,
		&AFacilityLight::StopFlicker,
		Duration,
		false
	);
}

void AFacilityLight::FlickerTick()
{
	bool bFlickerOn = FMath::RandBool();
	LightComponent->SetIntensity(bFlickerOn ? DefaultIntensity * FMath::FRandRange(0.2f, 1.0f) : 0.0f);
}

void AFacilityLight::StopFlicker()
{
	GetWorldTimerManager().ClearTimer(FlickerTimerHandle);

	if (bWasOnBeforeFlicker)
	{
		TurnOn();
	}
	else
	{
		TurnOff();
	}
}

void AFacilityLight::OnRep_IsOn()
{
	LightComponent->SetIntensity(bIsOn ? DefaultIntensity : 0.0f);
}
