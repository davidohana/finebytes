(function () {
  "use strict";

  /** @type {HTMLDivElement | null} */
  var overlay = null;

  /**
   * @returns {HTMLDivElement}
   */
  function ensureOverlay() {
    if (overlay) {
      return overlay;
    }

    overlay = document.createElement("div");
    overlay.className = "screenshot-lightbox";
    overlay.setAttribute("role", "dialog");
    overlay.setAttribute("aria-modal", "true");
    overlay.setAttribute("aria-label", "Full-size screenshot");
    overlay.innerHTML =
      '<button type="button" class="screenshot-lightbox-close" aria-label="Close">×</button>' +
      '<img alt="" />';

    overlay.addEventListener("click", function (event) {
      var target = /** @type {HTMLElement} */ (event.target);
      if (
        target === overlay ||
        target.tagName === "IMG" ||
        target.classList.contains("screenshot-lightbox-close")
      ) {
        closeLightbox();
      }
    });

    document.body.appendChild(overlay);
    return overlay;
  }

  /**
   * @param {HTMLImageElement} img
   */
  function openLightbox(img) {
    var box = ensureOverlay();
    var full = /** @type {HTMLImageElement} */ (box.querySelector("img"));
    full.src = img.currentSrc || img.src;
    full.alt = img.alt || "";
    box.classList.add("is-open");
    document.documentElement.classList.add("screenshot-lightbox-open");
    document.addEventListener("keydown", onKeyDown);
  }

  function closeLightbox() {
    if (!overlay || !overlay.classList.contains("is-open")) {
      return;
    }

    overlay.classList.remove("is-open");
    document.documentElement.classList.remove("screenshot-lightbox-open");
    var full = /** @type {HTMLImageElement} */ (overlay.querySelector("img"));
    full.removeAttribute("src");
    full.alt = "";
    document.removeEventListener("keydown", onKeyDown);
  }

  /**
   * @param {KeyboardEvent} event
   */
  function onKeyDown(event) {
    if (event.key === "Escape") {
      closeLightbox();
    }
  }

  document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll("figure.screenshot img").forEach(function (img) {
      if (!img.getAttribute("title")) {
        img.setAttribute("title", "Click to enlarge");
      }
    });
  });

  document.addEventListener("click", function (event) {
    var target = /** @type {HTMLElement} */ (event.target);
    var img = target.closest("figure.screenshot img");
    if (!img || img.closest(".screenshot-lightbox")) {
      return;
    }

    event.preventDefault();
    openLightbox(/** @type {HTMLImageElement} */ (img));
  });
})();
