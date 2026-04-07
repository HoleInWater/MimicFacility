// SpatialAudioProcessor.h — Implements the 3D Audio Master Equation for spatialized sound processing.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "UObject/NoExportTypes.h"
#include "SpatialAudioProcessor.generated.h"

/**
 * FSpatialAudioParams
 * Parameters fed into the 3D Audio Master Equation per sound source.
 *
 * Master Equation:
 * y_L,R(t) = [ 1/(1+kd^2) * e^(-a*d_occ) * x(t - r*sin(theta)/c) * (c+v_r)/(c+v_s) ]
 *            * h_L,R(t, theta, phi) + SUM_i[ x(t - d_i/c) ]
 */
USTRUCT(BlueprintType)
struct FSpatialAudioParams
{
	GENERATED_BODY()

	/** Distance from source to listener (cm, will be converted to meters internally). */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Audio|Spatial")
	float Distance;

	/** Distance falloff constant — controls how aggressively volume drops with distance. */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Audio|Spatial")
	float FalloffConstant;

	/** Occlusion absorption coefficient — how much material between source and listener absorbs sound. */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Audio|Spatial")
	float OcclusionCoefficient;

	/** Distance the sound travels through occluding material (cm). */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Audio|Spatial")
	float OcclusionDistance;

	/** Horizontal angle of the sound source relative to the listener (radians). */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Audio|Spatial")
	float Theta;

	/** Vertical angle of the sound source relative to the listener (radians). */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Audio|Spatial")
	float Phi;

	/** Radial velocity of the listener toward/away from the source (cm/s). */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Audio|Spatial")
	float ListenerVelocity;

	/** Radial velocity of the source toward/away from the listener (cm/s). */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Audio|Spatial")
	float SourceVelocity;

	/** Distances to reflecting surfaces for reverb calculation (cm). */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Audio|Spatial")
	TArray<float> ReflectionDistances;

	FSpatialAudioParams()
		: Distance(100.0f)
		, FalloffConstant(0.001f)
		, OcclusionCoefficient(0.0f)
		, OcclusionDistance(0.0f)
		, Theta(0.0f)
		, Phi(0.0f)
		, ListenerVelocity(0.0f)
		, SourceVelocity(0.0f)
	{}
};

/**
 * FSpatialAudioResult
 * Output from the 3D Audio Master Equation — per-ear attenuation and timing values.
 */
USTRUCT(BlueprintType)
struct FSpatialAudioResult
{
	GENERATED_BODY()

	/** Volume multiplier for the left ear (0.0 - 1.0). */
	UPROPERTY(BlueprintReadOnly, Category = "Audio|Spatial")
	float LeftVolume;

	/** Volume multiplier for the right ear (0.0 - 1.0). */
	UPROPERTY(BlueprintReadOnly, Category = "Audio|Spatial")
	float RightVolume;

	/** Interaural time delay — how much earlier/later the left ear hears vs right (seconds). */
	UPROPERTY(BlueprintReadOnly, Category = "Audio|Spatial")
	float InterauralTimeDelay;

	/** Doppler pitch multiplier (1.0 = no shift). */
	UPROPERTY(BlueprintReadOnly, Category = "Audio|Spatial")
	float DopplerPitchMultiplier;

	/** Combined attenuation from distance falloff and occlusion (0.0 - 1.0). */
	UPROPERTY(BlueprintReadOnly, Category = "Audio|Spatial")
	float Attenuation;

	/** Sum of reflection contributions (reverb wet level). */
	UPROPERTY(BlueprintReadOnly, Category = "Audio|Spatial")
	float ReflectionLevel;

	/** Delay times for each reflection (seconds). */
	UPROPERTY(BlueprintReadOnly, Category = "Audio|Spatial")
	TArray<float> ReflectionDelays;

	FSpatialAudioResult()
		: LeftVolume(1.0f)
		, RightVolume(1.0f)
		, InterauralTimeDelay(0.0f)
		, DopplerPitchMultiplier(1.0f)
		, Attenuation(1.0f)
		, ReflectionLevel(0.0f)
	{}
};

/**
 * USpatialAudioProcessor
 *
 * Implements the 3D Audio Master Equation:
 *
 *   y_L,R(t) = [ 1/(1+kd^2) * e^(-alpha*d_occ) * x(t - r*sin(theta)/c) * (c+v_r)/(c+v_s) ]
 *              * h_L,R(t, theta, phi) + SUM_i[ x(t - d_i/c) ]
 *
 * This processes spatial audio parameters into per-ear volume, timing, pitch,
 * and reverb values that can be applied to Unreal audio components.
 */
UCLASS(BlueprintType)
class MIMICFACILITY_API USpatialAudioProcessor : public UObject
{
	GENERATED_BODY()

public:
	USpatialAudioProcessor();

	/**
	 * Evaluate the 3D Audio Master Equation for a given set of spatial parameters.
	 * Returns per-ear volume, ITD, Doppler shift, attenuation, and reflection data.
	 */
	UFUNCTION(BlueprintCallable, Category = "Audio|Spatial")
	static FSpatialAudioResult ProcessSpatialAudio(const FSpatialAudioParams& Params);

	/**
	 * Compute spatial audio params from two actors (source and listener).
	 * Performs line traces for occlusion and reflection surface detection.
	 */
	UFUNCTION(BlueprintCallable, Category = "Audio|Spatial", meta = (WorldContext = "WorldContext"))
	static FSpatialAudioParams ComputeParamsFromActors(
		UObject* WorldContext,
		AActor* Source,
		AActor* Listener,
		float FalloffConstant = 0.001f,
		float OcclusionCoefficient = 0.5f
	);

	/** Apply a spatial audio result to an Unreal AudioComponent. */
	UFUNCTION(BlueprintCallable, Category = "Audio|Spatial")
	static void ApplyToAudioComponent(UAudioComponent* AudioComp, const FSpatialAudioResult& Result);

	// Physical constants (in Unreal units: cm and cm/s)
	static constexpr float SpeedOfSound = 34300.0f;  // cm/s (343 m/s)
	static constexpr float HeadRadius = 8.75f;        // cm (~0.0875m average human head radius)
};
