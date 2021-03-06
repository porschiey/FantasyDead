﻿(function () {

    var app = angular.module('wdf.settings', ['ngRoute']);

    app.controller('settingsController', ['$scope', '$rootScope', '$location', '$http', function ($scope, $rootScope, $location, $http) {
        document.addEventListener("deviceready", function () {
            $http.defaults.headers.common.Authorization = 'Bearer ' + $rootScope.user.Token;
       
            //tutorial page 1
            $scope.updateProfile = function () {

                $scope.tutorialPage = 2;
            };

            //go to next tutorial page (generic)
            $scope.nextPage = function () {

                $scope.tutorialPage++;
            };

            //configures push notifications
            $scope.startPushNotifications = function () {

                $scope.tutorialPage++;
            };

            $scope.rateApp = function () {
                $('#betaModal').modal();
                $rootScope.betaExit = new Date(new Date().getTime() + 5000);
                $rootScope.betaLoop();
            };

            //ends tutorial and marks user ready
            $scope.finishTutorial = function () {
                $rootScope.isNewUser = false;
                $rootScope.saveUserChanges();

                $location.path('/roster');
            };

            //checks to see if the username is valid
            $scope.checkingUsername = true;
            $scope.usernameValid = false;
            $scope.checkUsername = function () {
                $scope.checkingUsername = true;


                var alphaNumericReg = /[^\w ]/i;
                if ($scope.user.Username === null
                    || $scope.user.Username.length > 20
                    || $scope.user.Username.length < 3
                    || alphaNumericReg.test($scope.user.Username)) {
                    $scope.usernameMsg = 'Username must be between 3 and 20 characters, must be alphanumeric. Underscores "_" and spaces are allowed.';
                    $scope.checkingUsername = false;
                    $scope.usernameValid = false;
                    return;
                }


                var un = $rootScope.user.Username;
                $http.get($rootScope.fdApi + 'api/register/check/' + un).then(function (response) {
                    $scope.usernameValid = response.data;
                    $scope.usernameMsg = response.data ? 'That username is not taken.' : 'That username is already taken.';
                    $scope.checkingUsername = false;
                }).catch(function (error) {
                    $rootScope.handleError(error);
                    $scope.checkingUsername = false;
                    $rootScope.showError('There was a problem w/ checking your username.');
                });
            };

            $scope.emailValid = false;
            $scope.checkEmail = function () {
                var em = $rootScope.user.Email.trim();
                if (em === null)
                    return false;
                $scope.emailValid = (em !== ' ' && em !== '' && (em.indexOf('@') !== -1 && em.indexOf('.') !== -1));
            };

            $scope.changeEmail = function () {
                $('#changeEmailModal').modal();
            }

            $scope.saveEmail = function () {
                $('#changeEmailModal').modal('hide');
                var req = { Email: $rootScope.user.Email };
                $http.post($rootScope.fdApi + 'api/person/email', req).then(function (response) {
                    $rootScope.saveUserChanges();
                }).catch($rootScope.handleError);
            };

            //Finalizes registration, starts tutorial messages
            $scope.tutorialStep2 = function () {

                $rootScope.user.Role = 0;
                $rootScope.user.isNewUser = false;
              
                var req = { Email: $rootScope.user.Email, Username: $rootScope.user.Username };
                $http.post($rootScope.fdApi + 'api/person/email', req).then(function (response) {
                    $rootScope.saveUserChanges();
                }).catch($rootScope.handleError);

                $scope.tutorialStep2MessageLoop();
                $scope.nextPage();
            };

            $scope.dlh = $rootScope.generateDeadlineHoursOption();


            //tutorial loop tech
            var updateMsg = function (img, msg, contWith, delay) {
                $('#tutorial-text').fadeOut(250, function () {
                    $('#tutorial-text').text(msg);
                    $('#tutorial-text').fadeIn(250, function () {
                        setTimeout(contWith, delay);
                    });
                });
                $('#tutorial-img').fadeOut(250, function () {
                    $('#tutorial-img').attr('src', img);
                    $('#tutorial-img').fadeIn(250)
                });
            }

            var mPos = 0;
            var messages = [
                { i: 'images/tutorial/template.png', m: 'Each week you must slot characters from the show in your roster.', d: 6000 },
                { i: 'images/tutorial/template.png', m: 'They will earn you points by killing Walkers, saving allies, or even lying.', d: 7000 },
                { i: 'images/tutorial/template.png', m: 'You can even pick who you think might die.', d: 5000 },
                { i: 'images/tutorial/template.png', m: 'Choose carefully, however. You can only pick a character 3 times per 8 episodes.', d: 7000 },
                { i: 'images/tutorial/template.png', m: 'Within 24 hours of the episode airing, your points will be rewarded.', d: 8000 },
                { i: 'images/tutorial/template.png', m: 'Compete with your friends on the leaderboard, and earn rewards.', d: 6000 },
                { i: 'images/tutorial/template.png', m: 'Time to fill out your first roster...', d: 5000 }
            ];

            var cycle = function () {
                if (mPos === messages.length) {
                    $location.path('/roster');
                    if (!$scope.$$digest) $scope.$apply();
                    return;
                }

                updateMsg(messages[mPos].i, messages[mPos].m, cycle, messages[mPos].d);
                mPos++;
            };

            //starts the actual tutorial loop
            $scope.tutorialStep2MessageLoop = function () {
                setTimeout(cycle, 2000);
            };



            //uploading an avatar
            $rootScope.uploadImage = function (file, folder, contWith) {
                var uploadUrl = $rootScope.fdApi + 'api/configuration/image/' + folder;
                var fd = new FormData();
                fd.append('file', file);
                $http.post(uploadUrl, fd, {
                    transformRequest: angular.identity,
                    headers: { 'Content-Type': undefined, 'Authorization': 'Bearer ' + $rootScope.user.Token }
                }).then(function (response) {

                    if (typeof contWith === 'function')
                        contWith(response.data);
                }).catch(function (error) {
                    $('#avatarModal').modal('hide');
                    $scope.uploading = false;
                    $scope.showUpload = false;
                    $rootScope.handleError(error);
                    $rootScope.showError(error);
                });;
            };


            $scope.uploading = false;
            $scope.uploadFile = function () {
                $scope.uploading = true;
                var file = $scope.newImg;
                //send here
                $rootScope.uploadImage(file, 'avs', function (url) {
                    $scope.user.AvatarPictureUrl = url;
                    $scope.showUpload = false;
                    $('#avatarModal').modal('hide');
                    $scope.uploading = false;
                });

            };

            $scope.changeAvatar = function () {
                $('#avatarModal').modal('toggle');
            };


            //primary init method
            $scope.init = function () {
                if ($rootScope.user.isNewUser) {
                    $scope.tutorial = true;
                    $scope.tutorialPage = 1;
                    $scope.checkUsername();
                    $scope.checkEmail();
                }
            };


            $scope.init();
        });
    }]);


})();