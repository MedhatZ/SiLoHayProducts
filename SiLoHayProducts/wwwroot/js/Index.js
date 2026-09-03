var selectedProduct = {};
$(document).ready(function () {
    $("#productsContainer").on("click", ".openProductModal", function () 
    {

        selectedProduct.productId = $(this).data("product-id");
        selectedProduct.imageUrl = $(this).data("imageurl");
        selectedProduct.price = $(this).data("price");
        selectedProduct.name = $(this).data("name");
        selectedProduct.prepayment = $(this).data("prepayment");
        selectedProduct.isvailablenow = $(this).data("isvailablenow");

        // document.getElementById("email").value = "";
        // document.getElementById("fullname").value = "";
        // document.getElementById("phone").value = "";
        document.getElementById("divbuttons").style.visibility = "visible";

        //$("#fullname").removeAttr('required');​​​​​
        // document.getElementById("myForm").reset();

        $("#contactinfodiv").css("display", "block");
        $("#contactinfolabel").text("Info de contacto");
        $("#itemdetaildiv").css("display", "block");
        $("#product-id").text($(this).data("product-id"));
          $("#name").text($(this).data("name"));
          $("#price").text($(this).data("price"));
          $("#product-id").text($(this).data("product-id"));
          $('#imageurl').attr('src',$(this).data("imageurl"));

          if ($(this).data("isvailablenow") == "True")
          {
              ($(this).data("isvailablenow"));
              $("#modal-title").text("Listo para recoger hoy");
              document.getElementById("prepaymentlabel").style.visibility = "hidden";
          }
          else
          {
              $("#prepayment").text(selectedProduct.prepayment);
              document.getElementById("prepaymentlabel").style.visibility = "visible";
              $("#modal-title").text("");
          }
          $("#modal_dialog").dialog({
            modal: true,
            width: 400
        });
    });
});

$(document).ready(function () {
            // Initialize the validator
            var validator = $("#myForm").validate({
                // Prevent the default error message popup
                errorPlacement: function(error, element) {
                    error.appendTo(element.nextAll("span.field-validation-valid").first());
                }
            });

           $("#myForm").submit(function (event) {
                event.preventDefault(); // Prevent default form submission

                if ($("#myForm").valid()) {
                    $.ajax({
                        type: "POST",
                        url: "/Index?handler=Send",
                        data: {
                            "productId": selectedProduct.productId,
                            "name": selectedProduct.name,
                            "imageUrl": selectedProduct.imageUrl,
                            "fullname": $("#fullname").val(),
                            "phone": $("#phone").val(),
                            "email": $("#email").val()
                        },
                        beforeSend: function (xhr) {
                            xhr.setRequestHeader("XSRF-TOKEN",
                            $('input:hidden[name="__RequestVerificationToken"]').val());
                              $("#pleasewaitmodal").dialog({
                                modal: true,
                                width: 200
                            });

                            //$('#loading').show();  // show loading indicator
                            //$('#pleasewaitmodal').show();  // show loading indicator
                            //document.getElementById("divbuttons").style.visibility = "hidden";
                        },
                        success: function (response) {
                            $("#modal-title").text("");
                            $("#itemdetaildiv").css("display", "none");
                            $("#contactinfodiv").css("display", "none");
                            $("#contactinfolabel").html("Gracias por su interes. <br><br>Nos estaremos comunicando con usted lo antes posible.");
                            // document.getElementById('btnconfirm').style.display="none";  // for hide button
                            //document.getElementById("btnconfirm").style.visibility = "hidden";
                            document.getElementById("divbuttons").style.visibility = "hidden";
                            document.getElementById('lineseparator').style.display="none";
                        },
                        complete: function()
                        {
                            $("#pleasewaitmodal").dialog("close");
                            //$('#loading').hide();  // show loading indicator
                        },
                        error: function (error) {
                            // Handle error
                        }
                    });
                }
            });


            $("#btnclose").click(function(e) {
                e.preventDefault();
                validator.resetForm();
                $("#myForm")[0].reset();
                $(".field-validation-valid span").text('');
                $("#modal_dialog").dialog("close");
            });
        });
}