(function () {


    var app = angular.module('wdf.home', ['ngRoute']);

    app.controller('homeController', ['$scope', '$rootScope', '$location', '$http', function ($scope, $rootScope, $location, $http) {
        document.addEventListener("deviceready", function () {


            if ($rootScope.user.loggedIn)
                $location.path('/roster');

            //launches login modal
            $scope.getStarted = function () {
                $('#loginModal').modal();
            };



            $scope.regValid = true;
            $scope.logValid = true;
            $scope.validateReg = function () {
                $scope.regValid = true;
                $scope.regError = '';

                if ($scope.reg.email === '' || $scope.reg.email.indexOf('@') === -1 || $scope.reg.email.indexOf('.') === -1) {
                    $scope.regValid = false;
                    $scope.regError = 'Please enter a valid email address.';
                    return;
                }

                if ($scope.reg.p1 === '' || $scope.reg.p1.length < 8) {
                    $scope.regValid = false;
                    $scope.regError = 'Passwords must be at least 8 characters.';
                    return;
                }

                if ($scope.reg.p1 !== $scope.reg.p2) {
                    $scope.regValid = false;
                    $scope.regError = 'Passwords do not match.';
                    return;
                }
            };


            $scope.submitting = false;
            $scope.submitReg = function () {
                $scope.submitting = true;
                var regReq = {
                    Username: $scope.reg.email.split('@')[0],
                    Email: $scope.reg.email,
                    SocialIdentity: {
                        PlatformUserId: $scope.reg.email,
                        PlatformName: 'Custom',
                        Credentials: btoa($scope.reg.p2)
                    }
                };

                $http.put($rootScope.fdApi + 'api/register', regReq).then(function (response) {
                    $rootScope.authorize(response.data);
                    $('#loginModal').modal('hide');
                }).catch(function (error) {
                    $scope.regValid = false;
                    $scope.submitting = false;
                    $scope.regError = error.data.Message;
                });
            };

            $scope.submitLog = function () {
                $scope.submitting = true;
                var logReq = {
                    PlatformUserId: $scope.log.email,
                    PlatformName: 'Custom',
                    Credentials: btoa($scope.log.p1)
                };

                $http.put($rootScope.fdApi + 'api/register/login', logReq).then(function (response) {
                    $rootScope.authorize(response.data);
                    $('#loginModal').modal('hide');
                }).catch(function (error) {
                    $scope.logValid = false;
                    $scope.submitting = false;
                    $scope.logError = error.data.Message;
                });
            };
        });
    }]);


})();