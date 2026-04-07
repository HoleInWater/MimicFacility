// SpatialAudioProcessor.cpp — 3D Audio Master Equation implementation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "SpatialAudioProcessor.h"
#include "Components/AudioComponent.h"
#include "Kismet/GameplayStatics.h"
#include "CollisionQueryParams.h"
#include "Engine/World.h"

USpatialAudioProcessor::USpatialAudioProcessor()
{
}

FSpatialAudioResult USpatialAudioProcessor::ProcessSpatialAudio(const FSpatialAudioParams& Params)
{
	FSpatialAudioResult Result;

	const float d = Params.Distance;
	const float k = Params.FalloffConstant;
	const float Alpha = Params.OcclusionCoefficient;
	const float d_occ = Params.OcclusionDistance;
	const float Theta = Params.Theta;
	const float v_r = Params.ListenerVelocity;
	const float v_s = Params.SourceVelocity;
	const float c = SpeedOfSound;
	const float r = HeadRadius;

	// === Component 1: Distance attenuation — 1 / (1 + k*d^2) ===
	float DistanceAttenuation = 1.0f / (1.0f + k * d * d);

	// === Component 2: Occlusion absorption — e^(-alpha * d_occ) ===
	float OcclusionAttenuation = FMath::Exp(-Alpha * d_occ);

	// Combined attenuation
	Result.Attenuation = DistanceAttenuation * OcclusionAttenuation;

	// === Component 3: Interaural Time Delay — r * sin(theta) / c ===
	// Positive = sound reaches left ear first, negative = right ear first
	Result.InterauralTimeDelay = r * FMath::Sin(Theta) / c;

	// === Component 4: Doppler effect — (c + v_r) / (c + v_s) ===
	// Guard against division by zero (source at speed of sound)
	float DopplerDenom = c + v_s;
	if (FMath::Abs(DopplerDenom) < 1.0f)
	{
		DopplerDenom = 1.0f; // Clamp to prevent extreme values
	}
	Result.DopplerPitchMultiplier = (c + v_r) / DopplerDenom;
	Result.DopplerPitchMultiplier = FMath::Clamp(Result.DopplerPitchMultiplier, 0.5f, 2.0f);

	// === Component 5: Per-ear volume (simplified HRTF approximation) ===
	// Full HRTF convolution requires impulse response data — this is a
	// level/panning approximation suitable for gameplay audio.
	//
	// Left ear: louder when theta > 0 (source on the left)
	// Right ear: louder when theta < 0 (source on the right)
	float Pan = FMath::Sin(Theta); // -1 (full right) to +1 (full left)

	// Apply head shadow — frequencies are attenuated on the far side of the head.
	// Higher attenuation for the shadowed ear.
	float HeadShadow = FMath::Clamp(0.6f + 0.4f * FMath::Abs(Pan), 0.6f, 1.0f);

	Result.LeftVolume  = Result.Attenuation * FMath::Clamp(0.5f + 0.5f * Pan, 0.0f, 1.0f) * HeadShadow;
	Result.RightVolume = Result.Attenuation * FMath::Clamp(0.5f - 0.5f * Pan, 0.0f, 1.0f) * HeadShadow;

	// === Component 6: Reflections — SUM_i[ x(t - d_i/c) ] ===
	// Each reflecting surface contributes a delayed, attenuated copy of the signal.
	Result.ReflectionLevel = 0.0f;
	for (float ReflDist : Params.ReflectionDistances)
	{
		float ReflDelay = ReflDist / c;
		float ReflAttenuation = 1.0f / (1.0f + k * ReflDist * ReflDist);
		Result.ReflectionDelays.Add(ReflDelay);
		Result.ReflectionLevel += ReflAttenuation;
	}
	// Normalize reflection level to 0-1 range
	if (Params.ReflectionDistances.Num() > 0)
	{
		Result.ReflectionLevel /= Params.ReflectionDistances.Num();
	}

	return Result;
}

FSpatialAudioParams USpatialAudioProcessor::ComputeParamsFromActors(
	UObject* WorldContext,
	AActor* Source,
	AActor* Listener,
	float FalloffConstant,
	float OcclusionCoefficient)
{
	FSpatialAudioParams Params;

	if (!Source || !Listener || !WorldContext) return Params;

	UWorld* World = WorldContext->GetWorld();
	if (!World) return Params;

	FVector SourceLoc = Source->GetActorLocation();
	FVector ListenerLoc = Listener->GetActorLocation();
	FVector ToSource = SourceLoc - ListenerLoc;

	// Distance
	Params.Distance = ToSource.Size();
	Params.FalloffConstant = FalloffConstant;

	// Angles relative to listener's forward direction
	FRotator ListenerRot = Listener->GetActorRotation();
	FVector ListenerForward = ListenerRot.Vector();
	FVector ListenerRight = FRotationMatrix(ListenerRot).GetUnitAxis(EAxis::Y);
	FVector ListenerUp = FRotationMatrix(ListenerRot).GetUnitAxis(EAxis::Z);

	FVector ToSourceNorm = ToSource.GetSafeNormal();

	// Theta: horizontal angle (azimuth) — positive = source on listener's left
	Params.Theta = FMath::Atan2(FVector::DotProduct(ToSourceNorm, ListenerRight),
	                             FVector::DotProduct(ToSourceNorm, ListenerForward));

	// Phi: vertical angle (elevation)
	Params.Phi = FMath::Asin(FMath::Clamp(FVector::DotProduct(ToSourceNorm, ListenerUp), -1.0f, 1.0f));

	// Doppler — radial velocity components
	FVector SourceVel = Source->GetVelocity();
	FVector ListenerVel = Listener->GetVelocity();
	if (Params.Distance > KINDA_SMALL_NUMBER)
	{
		FVector Dir = ToSource / Params.Distance;
		Params.SourceVelocity = FVector::DotProduct(SourceVel, -Dir);    // positive = approaching
		Params.ListenerVelocity = FVector::DotProduct(ListenerVel, Dir); // positive = approaching
	}

	// Occlusion — line trace from listener to source
	Params.OcclusionCoefficient = OcclusionCoefficient;
	Params.OcclusionDistance = 0.0f;

	FHitResult OccHit;
	FCollisionQueryParams TraceParams;
	TraceParams.AddIgnoredActor(Source);
	TraceParams.AddIgnoredActor(Listener);

	if (World->LineTraceSingleByChannel(OccHit, ListenerLoc, SourceLoc, ECC_Visibility, TraceParams))
	{
		// Something is between listener and source — estimate occlusion thickness
		// Trace from both directions and estimate material thickness
		Params.OcclusionDistance = FMath::Max(10.0f, (SourceLoc - OccHit.ImpactPoint).Size() * 0.1f);
	}

	// Reflections — trace to nearby surfaces (6 cardinal directions)
	TArray<FVector> TraceDirections = {
		FVector::ForwardVector, FVector::BackwardVector,
		FVector::RightVector, FVector::LeftVector,
		FVector::UpVector, FVector::DownVector
	};

	float MaxReflectionDist = 2000.0f; // 20 meters

	for (const FVector& Dir : TraceDirections)
	{
		FHitResult ReflHit;
		if (World->LineTraceSingleByChannel(ReflHit, ListenerLoc, ListenerLoc + Dir * MaxReflectionDist, ECC_Visibility, TraceParams))
		{
			// Reflection distance = listener->wall + wall->source (simplified)
			float WallDist = ReflHit.Distance;
			float ReflDist = WallDist * 2.0f; // Simplified: assume source is roughly same distance from wall
			Params.ReflectionDistances.Add(ReflDist);
		}
	}

	return Params;
}

void USpatialAudioProcessor::ApplyToAudioComponent(UAudioComponent* AudioComp, const FSpatialAudioResult& Result)
{
	if (!AudioComp) return;

	// Apply attenuation as volume multiplier
	float AvgVolume = (Result.LeftVolume + Result.RightVolume) * 0.5f;
	AudioComp->SetVolumeMultiplier(AvgVolume);

	// Apply Doppler as pitch multiplier
	AudioComp->SetPitchMultiplier(Result.DopplerPitchMultiplier);

	// Reverb/reflection level can be applied via sound class or submix effect in full implementation
	// For now, log it for testing
	UE_LOG(LogTemp, Verbose, TEXT("SpatialAudio — Vol: L=%.3f R=%.3f | Doppler: %.3f | Atten: %.3f | ITD: %.6fs | Refl: %.3f"),
		Result.LeftVolume, Result.RightVolume,
		Result.DopplerPitchMultiplier,
		Result.Attenuation,
		Result.InterauralTimeDelay,
		Result.ReflectionLevel);
}
