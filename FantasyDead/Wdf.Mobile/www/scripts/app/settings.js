(function () {

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

            //ends tutorial and marks user ready
            $scope.finishTutorial = function () {
                $rootScope.isNewUser = false;
                $rootScope.saveUserChanges();

                $location.path('/roster');
            };


            $scope.checkingUsername = true;
            $scope.usernameValid = false;
            $scope.checkUsername = function () {
                $scope.checkingUsername = true;

                var alphaNumericReg = /[^\w ]/i;
                if ($scope.user.Username.length > 20 || $scope.user.Username.length < 3 || alphaNumericReg.test($scope.user.Username)) {
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
                var em = $rootScope.user.Email;
                $scope.emailValid = (em !== '' && (em.indexOf('@') !== -1 && em.indexOf('.') !== -1));
            };


            $scope.tutorialStep2 = function () {

                $rootScope.user.Role = 0;
                $rootScope.user.isNewUser = false;
                $rootScope.saveUserChanges();

                $http.post($rootScope.fdApi + 'api/person/config/username', $rootScope.user.Username).then(function (response) {
                }).catch($rootScope.handleError);

                $http.post($rootScope.fdApi + 'api/person/config/email', $rootScope.user.Email).then(function (response) {
                }).catch($rootScope.handleError);

                $scope.tutorialStep2MessageLoop();
                $scope.nextPage();
            };


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

            $scope.tutorialStep2MessageLoop = function () {
                setTimeout(cycle, 2000);
            };



            //toggling notifications.
            $scope.toggleNotifications = function () {

                $rootScope.user.Configuration.ReceiveNotifications = !$rootScope.user.Configuration.ReceiveNotifications;

                if ($rootScope.user.Configuration.ReceiveNotifications) {
                    $rootScope.setupPushNotification();
                }
                else {
                    $rootScope.destroyPushSetup();
                }
            };

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


            $scope.init = function () {
                if ($rootScope.user.isNewUser) {
                    $scope.tutorial = true;
                    $scope.tutorialPage = 1;
                    $scope.checkUsername();
                    $scope.checkEmail();
                }
            };

            $scope.preLockHours = [
                { hours: 1, d: '1 Hour Before' },
                { hours: 2, d: '2 Hours Before' },
                { hours: 4, d: '4 Hours Before' },
                { hours: 6, d: '6 Hours Before' },
                { hours: 12, d: '12 Hours Before' },
                { hours: 24, d: '24 Hours Before' },
                { hours: 48, d: '48 Hours Before' },
                { hours: 72, d: '72 Hours Before' }
            ];


            $scope.updateConfiguration = function (key, value) {
                $http.post($rootScope.fdApi + 'api/person/config/' + key, value).then(function (response) {
                }).catch(function (error) {
                    $rootScope.handleError(error);
                    $rootScope.showError(error);
                });
            };

            $scope.init();
        });
    }]);


})();