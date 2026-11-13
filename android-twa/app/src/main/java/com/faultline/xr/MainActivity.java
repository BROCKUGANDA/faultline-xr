package com.faultline.xr;

import android.app.Activity;
import android.content.Intent;
import android.net.Uri;
import android.os.Bundle;
import android.view.View;
import android.view.ViewGroup;
import android.webkit.WebChromeClient;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.TextView;

/**
 * FAULTLINE XR launcher.
 *
 * FAULTLINE XR is a WebXR experience. Android WebView does NOT implement the
 * WebXR Device API, so an in-app WebView can only present the desktop-preview
 * flow. The immersive hand-tracking experience must run in a WebXR-capable
 * browser (Meta Quest Browser on a headset, or Chrome for WebXR-capable
 * Android devices).
 *
 * This activity therefore does two things:
 *   1. Presents the experience in a WebView for review / non-XR devices.
 *   2. Offers an explicit hand-off to the device browser for WebXR, which is
 *      the path that produces the judged experience on a Quest headset.
 */
public class MainActivity extends Activity {

    /**
     * Public HTTPS URL of the built experience. Injected at build time by
     * scripts/build-twa-apk.mjs from the resolved host, so the same source
     * produces a working APK for any deployment host.
     */
    private static final String URL = LaunchUrl.get();

    private WebView webView;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        LinearLayout root = new LinearLayout(this);
        root.setOrientation(LinearLayout.VERTICAL);
        root.setLayoutParams(new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT));

        webView = new WebView(this);
        webView.setLayoutParams(new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, 0, 1f));
        configureWebView(webView);
        root.addView(webView);

        LinearLayout bar = new LinearLayout(this);
        bar.setOrientation(LinearLayout.HORIZONTAL);
        bar.setPadding(16, 24, 16, 24);
        bar.setBackgroundColor(0xFF070B12);

        TextView status = new TextView(this);
        // State the limitation on the surface rather than letting the user
        // discover it after tapping around: Android WebView has no WebXR.
        status.setText(R.string.preview_banner);
        status.setTextColor(0xFF91A4BA);
        status.setTextSize(12f);
        status.setLayoutParams(new LinearLayout.LayoutParams(0,
                ViewGroup.LayoutParams.WRAP_CONTENT, 1f));
        bar.addView(status);

        Button openInVr = new Button(this);
        openInVr.setText("Open in VR browser");
        openInVr.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                launchExternal(URL);
            }
        });
        bar.addView(openInVr);

        root.addView(bar);
        setContentView(root);

        if (savedInstanceState == null) {
            webView.loadUrl(URL);
        }
    }

    private void configureWebView(WebView view) {
        WebSettings settings = view.getSettings();
        settings.setJavaScriptEnabled(true);
        settings.setDomStorageEnabled(true);
        settings.setDatabaseEnabled(true);
        settings.setAllowFileAccess(false);
        settings.setAllowContentAccess(false);
        settings.setMediaPlaybackRequiresUserGesture(false);
        // WebGL needs hardware acceleration, which is on by default at the
        // application level; these keeps the surface composited correctly.
        view.setLayerType(View.LAYER_TYPE_HARDWARE, null);
        view.setWebChromeClient(new WebChromeClient());
        view.setWebViewClient(new WebViewClient() {
            @Override
            public boolean shouldOverrideUrlLoading(WebView view, String url) {
                // Keep the experience in-app; hand off only non-http schemes.
                if (url == null) {
                    return true;
                }
                if (url.startsWith("http://") || url.startsWith("https://")) {
                    return false;
                }
                try {
                    startActivity(new Intent(Intent.ACTION_VIEW, Uri.parse(url)));
                } catch (Exception ignored) {
                    // No handler for the scheme; ignore rather than crash.
                }
                return true;
            }
        });
    }

    /** Hand the WebXR experience to a browser that implements the Device API. */
    private void launchExternal(String url) {
        Intent intent = new Intent(Intent.ACTION_VIEW, Uri.parse(url));
        intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        try {
            startActivity(intent);
        } catch (Exception error) {
            // No browser at all: fall back to the in-app preview, which is
            // already loaded, rather than closing the experience.
            if (webView != null) {
                webView.loadUrl(url);
            }
        }
    }

    @Override
    public void onBackPressed() {
        if (webView != null && webView.canGoBack()) {
            webView.goBack();
            return;
        }
        super.onBackPressed();
    }
}