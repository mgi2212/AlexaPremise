var simplePage = (function() {
    /**
     *  Download LWA SDK and set client.
     */
    window.onAmazonLoginReady = function() {
        amazon.Login.setClientId('amzn1.application-oa2-client.b659c342aba9405eb5afc9509716d51f');
    };
    (function(d) {
        var a = d.createElement('script'); a.type = 'text/javascript';
        a.async = true; a.id = 'amazon-login-sdk';
        a.src = 'https://api-cdn.amazon.com/sdk/login1.js';
        d.getElementById('amazon-root').appendChild(a);
    })(document);

    var customerId; // variable to hold CustomerId.
    
    /**
     *  Enable Bootstrap's tool tips, set customerId holder to empty, and setup Bootstrap alert.
     */
    $(document).ready(function(){
        $('[data-toggle="tooltip"]').tooltip();
        customerId = '';
        $(".alert").alert()
    });
    
    /**
     *  Initialize API Gateway Client
     */
    var apigClient = apigClientFactory.newClient({
     apiKey: '4agggHAInL9ZdkosPF2v08ZOPFeNA9Rk6Euq9p1B'
    });

    /**
     *  Add click event handeler for Login With Amazon component.
     */
    $('#LoginWithAmazon').click(function() {
      setTimeout(window.doLogin, 1000);
      return false;
    });
    
    /**
     *  Add click event handeler for log out functionality.
     */
    $('#Logout').click(function() {
      amazon.Login.logout();
      showLogon();
      return false;
    });
    
    /**
     * Set click handler for submitting the registration data.
     */
    $('#submitButton').click(function() {
        submitData();
        $(".form-control-editable").prop("readonly", true);
        $('#submitButton').hide();
        $('#resetButton').hide();
        $('#editButton').show();
        return false;
    });

    /**
     * Set click handler for editing the registration data.
     */
    $('#editButton').click(function() {
        $(".form-control-editable").prop("readonly", false);
        $('#submitButton').show();
        $('#resetButton').show();
        $('#editButton').hide();
        return false;
    });

    /**
     * Set click handler for resetting the registration data (locally).
     */
    $('#resetButton').click(function() {
        $('.form-control-editable').val('');
        return false;
    });
    
    /**
     * Performs the action of logging into Amazon LWA.
     */
    window.doLogin = function() {
     options = { scope: 'profile' };
     amazon.Login.authorize(options, function(response) {
        if ( response.error ) {
            attachAlert('<strong>oauth error!</strong> ' + response.error);
            return;
        }
        amazon.Login.retrieveProfile(response.access_token, function(response) {
           showForm(response);

           if ( window.console && window.console.log )
              window.console.log(response);
        });
     });
    };

    /**
     * Performs the view manipulation for showing log in component.
     */
    var showLogon = function() {
       $('#pagedata').hide();       //Hide the registration component.
       $('.form-control').val('');  //Empty the input fields.
       customerId = '';             //Empty the persisted customerId.
       $('.navbar-header').hide();  //Hide the hamburger menu button.
       $('#NavItems').hide();       //Hide the navigation links.
       $('#login').show();          //Show the login button.
       $('#footer').show();         //Show the privacy policy link footer.
    }
    
    /**
     * Performs the view manipulation for entering the registration data component.
     */        
    var showForm = function(response) {
       $('#login').hide();                                      //Hide the log in button.
       $('.navbar-header').show();                              //Show the hamburger menu button.
       $('#nameInput').val(response.profile.Name);              //Fill name input (read-only).
       $('#emailInput').val(response.profile.PrimaryEmail);     //Fill email input (read-only).
       customerId = response.profile.CustomerId;                //Store the CustomerId.
       getAndUpdateData(response.profile.CustomerId);           //Get and fill other inputs (if previously saved).
       $('#NavItems').show();                                   //Show the navigation links.
       $('#pagedata').show();                                   //Show the registration component.
       $('#editButton').hide();                                 //Hide the edit button.
       $('#footer').hide();                                     //Hide the privacy policy link footer.
    }

    /**
     * Performs the view manipulation for getting the registration data component.
     */
    var updateFormData = function(result) {
        $(".form-control-editable").prop("readonly", true);
        $('#submitButton').hide();
        $('#resetButton').hide();
        $('#editButton').show();
        $('#hostInput').val(result.data.Item.host.S);
        $('#pathInput').val(result.data.Item.app_path.S);
        $('#portInput').val(result.data.Item.port.S);
        $('#tokenInput').val(result.data.Item.access_token.S);
    }

    /**
     * Performs the view manipulation for editing the registration data component.
     */
    var prepareFormEntry = function() {
        $(".form-control-editable").prop("readonly", false);
        $('#submitButton').show();
        $('#resetButton').show();
    }

    /**
     * Performs the action and view manipulation for getting the registration data.
     */
    var getAndUpdateData = function(customerId) {
       var body = {
         "function": "getItem",
         "id": customerId
       };

       apigClient.premiseDBMicroservicePost({}, body)
          .then(function(result){
             if (result.data.Item) {
               updateFormData(result);
             } else {
               prepareFormEntry();
             }}).catch(function(result){
               attachAlert('<strong>something went wrong; sorry!</strong> Please try back in a little bit.');
             });
    }

    /**
     * Performs the action and view manipulation for submitting the registration data.
     */
    var submitData = function() {
        var id = customerId;
        if (!id || id == '') {
            alert("Something went wrong! Please logout, then log back in.");
            return;
        }
        var body = {
            "function": "putItem",
            "id": id,
            "host": $('#hostInput').val(),
            "port": $('#portInput').val(),
            "app_path": $('#pathInput').val(),
            "access_token": $('#tokenInput').val()
        };

        apigClient.premiseDBMicroservicePost({}, body)
            .then(function(result){
                return;	
            }).catch(function(result){
                attachAlert('<strong>something went wrong; sorry!</strong> Please try back in a little bit.');
            });
    }
    
    /**
     * Attaches an alert (Bootstrap, alert-danger) with a message and a close button.
     */
    var attachAlert = function(messageHtml) {
        $('#alertContainer').html('<div id="alert" class="alert alert-danger alert-dismissible show" role="alert">' + 
                                  '   <button type="button" class="close" data-dismiss="alert" aria-label="Close">' +
                                  '      <span aria-hidden="true">&times;</span>' + 
                                  '    </button>' +
                                  '    <span id="alertMessage"></span>' + 
                                  '</div>');
        $('#alertMessage').html(messageHtml);
    }
})();