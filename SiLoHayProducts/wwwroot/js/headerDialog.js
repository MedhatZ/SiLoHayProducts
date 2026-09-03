// ================================
//  GLOBAL ORIGINAL HTML SNAPSHOT
// ================================
var originalModalHtml = "";
var originalHeaderHtml = "";

// ================================
//  Email validation
// ================================
function isValidEmail(email) {
    var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}

// ================================
//  Show errors
// ================================
function showValidationErrors(errors) {
    $(".field-validation-error").text("");
    $(".input-validation-error").removeClass("input-validation-error");
    $("#validationSummary").hide().empty();

    if (errors.length > 0) {
        var errorList = $("<ul>");
        errors.forEach(function (error) {
            errorList.append($("<li>").text(error));
        });
        $("#validationSummary").html(errorList).show();
    }

    $("#headerInfoDialog").dialog("option", "height", "auto");
}

// ================================
// Validate + reCAPTCHA
// ================================

 
function submitHeaderForm() {

    $(".field-validation-error").text("");
    $(".input-validation-error").removeClass("input-validation-error");
    $("#validationSummary").hide().empty();

    var full = $("#hdrFullname").val().trim();
    var email = $("#hdrEmail").val().trim();
    var phone = $("#hdrPhone").val().trim();
    var item = $("#hdrItem").val().trim();

    var errors = [];

    if (!full) {
        errors.push("Nombre requerido.");
        $("#hdrFullname").addClass("input-validation-error");
    }

    if (!email) {
        errors.push("Correo electrónico requerido.");
        $("#hdrEmail").addClass("input-validation-error");
    } else if (!isValidEmail(email)) {
        errors.push("Correo electrónico inválido.");
        $("#hdrEmail").addClass("input-validation-error");
    }

    if (!phone) {
        errors.push("Teléfono requerido.");
        $("#hdrPhone").addClass("input-validation-error");
    }

    if (!item) {
        errors.push("Descripción del artículo requerida.");
        $("#hdrItem").addClass("input-validation-error");
    }

    if (errors.length > 0) {
        showValidationErrors(errors);
        return false;
    }

    // Execute recaptcha
    grecaptcha.ready(function () {
        grecaptcha.execute(window.RECAPTCHA_SITE_KEY, { action: "submit" })
            .then(function (token) {
                $("#recaptchaResponse").val(token);
                sendHeaderAjax();
            });
    });

    return false;
}

// ================================
// AJAX SUBMIT
// ================================
function sendHeaderAjax() {

    $("#hdrBtnSend").prop("disabled", true).text("Enviando...");

    $.ajax({
        url: $("#headerForm").attr("action"),
        type: "POST",
        data: $("#headerForm").serialize(),

        success: function (response) {

            let res = null;

            // Try to parse JSON safely
            try {
                res = (typeof response === "string") ? JSON.parse(response) : response;
            } catch (e) {
                // Response is not JSON → treat as success to avoid UI errors
                res = { success: true };
            }

            // -----------------------------------
            // Success case
            // -----------------------------------
            if (res && res.success === true) {

                // Hide the header section
                $(".desktop-modal-header").hide();

                // Show success message
                $("#modalBodyContent").html(`
                    <div style="text-align:center;padding:40px 20px;">
                        <h3 style="color:#2A8703;font-size:20px;margin-bottom:15px;">
                            ¡Gracias por su interés!
                        </h3>
                        <p style="font-size:15px;color:#444;">
                            Nos estaremos comunicando lo antes posible.
                        </p>

                        <button id="closeSuccessBtn"
                            style="margin-top:25px;padding:10px 20px;background:#007bff;
                                border:none;color:#fff;border-radius:4px;cursor:pointer;font-size:16px;">
                            Cerrar
                        </button>
                    </div>
                `);

            }
            // -----------------------------------
            // Error case from server
            // -----------------------------------
            else {

                $("#hdrBtnSend").prop("disabled", false).text("Enviar correo");

                showValidationErrors([
                    (res && res.message)
                        ? res.message
                        : "Error al enviar el formulario."
                ]);
            }
        },

        // -----------------------------------
        // AJAX error case
        // -----------------------------------
        error: function (xhr, status, error) {
            alert("Hubo un error enviando el formulario: " + error);
            $("#hdrBtnSend").prop("disabled", false).text("Enviar correo");
        }
    });
}

// ================================
// INIT DIALOG
// ================================
function initHeaderDialog() {

    var $dlg = $("#headerInfoDialog");

    // Create dialog
    $dlg.dialog({
        modal: true,
        autoOpen: false,
        width: 420,
        maxWidth: "100%",
        draggable: false,
        resizable: false,
        appendTo: "body",
        open: function () {
            $("body").addClass("modal-open");   //   scroll
            resetForm();
            $(".desktop-modal-header").show();
},
        close: function () {
            $("body").removeClass("modal-open"); //  scroll
            resetForm();
            $(".desktop-modal-header").show();
}
    });

    // Restore full original content
    function restoreOriginalContent() {
        $("#modalBodyContent").html(originalModalHtml);

        if ($(".desktop-modal-header").length === 0) {
            $("#modalBodyContent").before(originalHeaderHtml);
        }
    }

    // RESET FORM
    function resetForm() {
        restoreOriginalContent();
        $("#headerForm")[0].reset();
        $("#validationSummary").hide().empty();
        $(".input-validation-error").removeClass("input-validation-error");
        $("#hdrBtnSend").prop("disabled", false).text("Enviar correo");
    }

    // OPEN
    $(document).on("click", "#emailLink", function (e) {
        e.preventDefault();
        restoreOriginalContent();
        $dlg.dialog("open");
    });

    // CLOSE FORM
    $(document).on("click", "#hdrBtnClose", function () {
        $dlg.dialog("close");
    });

    // CLOSE SUCCESS
    $(document).on("click", "#closeSuccessBtn", function () {
        $dlg.dialog("close");
    });

    // SUBMIT
    $(document).on("click", "#hdrBtnSend", function (e) {
        e.preventDefault();
        submitHeaderForm();
    });

    // Live validation
    $(document).on("blur", "#hdrEmail", function () {
        var email = $(this).val().trim();
        if (email && !isValidEmail(email)) {
            $(this).addClass("input-validation-error");
            $("[data-valmsg-for='email']").text("Email inválido");
        }
    });

    $(document).on("input", "#hdrFullname,#hdrEmail,#hdrPhone,#hdrItem", function () {
        $(this).removeClass("input-validation-error");
        var f = $(this).attr("name");
        if (f) $("[data-valmsg-for='" + f + "']").text("");
        $("#validationSummary").hide().empty();
    });

    $(document).on("input", "#hdrPhone", function () {
        this.value = this.value.replace(/[^0-9]/g, "");
    });
}

// ================================
// READY
// ================================
$(document).ready(function () {

    if ($("#headerInfoDialog").length > 0) {

        // Save original content ONCE
        originalModalHtml = $("#modalBodyContent").html();
        originalHeaderHtml = $(".desktop-modal-header").prop("outerHTML");

        initHeaderDialog();
    }
});
