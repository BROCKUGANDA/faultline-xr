package com.faultline.xr;

/**
 * Build-time injected launch URL.
 *
 * scripts/build-twa-apk.mjs rewrites the single return statement in this class
 * before compilation, so the APK always points at the resolved public HTTPS URL
 * without needing the Gradle Android plugin's BuildConfig generation.
 */
final class LaunchUrl {

    private LaunchUrl() {
    }

    static String get() {
        return "https://PLACEHOLDER_HOST/";
    }
}