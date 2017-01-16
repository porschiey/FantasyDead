(function () {

    var defaultUser = { username: 'guest', loggedIn: false, id: '', key: '', isNewUser: false }; //default

    var loginOptions = { redirect_uri: 'https://thefantasydead.com/callback' };

    var app = angular.module('wdf', ['ngCordova', 'ngCordovaOauth', 'wdf.home', 'wdf.roster', 'wdf.settings', 'wdf.leaderboard', 'wdf.stats', 'wdf.friends']);

    app.directive('fileModel', ['$parse', function ($parse) {
        return {
            restrict: 'A',
            link: function (scope, element, attrs) {
                var model = $parse(attrs.fileModel);
                var modelSetter = model.assign;

                element.bind('change', function () {
                    scope.$apply(function () {
                        modelSetter(scope, element[0].files[0]);
                    });
                });
            }
        };
    }]);

    app.run(['$rootScope', '$cordovaOauth', '$location', '$http', function ($rootScope, $cordovaOauth, $location, $http) {
        document.addEventListener("deviceready", function () {

            //$rootScope.fdApi = 'http://192.168.1.2/';
            $rootScope.fdApi = 'https://thefantasydead.com/';
            $rootScope.loading = true;
            var init = function () {

                //discover login state.                
                var uString = localStorage.getItem('user');
                $rootScope.user = defaultUser; //default


                if (!uString) { //not logged in
                    $location.path('/home');
                } else {
                    try {
                        $rootScope.user = JSON.parse(uString);
                        $rootScope.user.isNewUser = $rootScope.user.Role === 1;
                        if ($rootScope.user.isNewUser)
                            $location.path('/settings');
                        else
                            $location.path('/roster');
                        console.log('user is already logged in');
                        console.log('user...: ' + JSON.stringify($rootScope.user));
                        $http.defaults.headers.common.Authorization = 'Bearer ' + $rootScope.user.Token;

                        $rootScope.deadlineLockHours = null;
                        if ($rootScope.user.Configuration.DeadlineReminderHours > 0)
                            $rootScope.deadlineLockHours = { hours: $rootScope.user.Configuration.DeadlineReminderHours, d: $rootScope.user.Configuration.DeadlineReminderHours + ' hour(s) before' };

                    } catch (e) {
                        console.error(JSON.stringify(e));
                        //reset and send home
                        localStorage.clear();
                        $location.path('/home');
                    }

                    if ($rootScope.user.Configuration.ReceiveNotifications)
                        $rootScope.setupPushNotification();
                }

                $rootScope.deviceReady = true;
                $rootScope.loading = false;

                var currentPlat = device.platform.toLowerCase();

                //setup push here


                //house keeping
                if (currentPlat === 'android') {
                    window.plugins.headerColor.tint('#000000');
                }

                if (!$rootScope.$$phase) $rootScope.$digest();
            };


            $rootScope.destroyPushSetup = function () {
                $http.get($rootScope.fdApi + 'api/person/push/cancel').then(function (response) {
                    $rootScope.saveUserChanges();
                }).catch($rootScope.handleError);
            };

            var pushOpts = {
                android: {
                    senderID: '20690878081'
                }
            };

            $rootScope.setupPushNotification = function () {

                PushNotification.hasPermission(function (perm) {
                    if (perm.isEnabled) {
                        var pN = PushNotification.init(pushOpts);

                        pN.on('registration', function (data) {

                            var pushReq = { device: 'android', registrationId: data.registrationId };
                            $http.put($rootScope.fdApi + 'api/person/push/register', pushReq).then(function (response) {
                                $rootScope.user.PushRegistration = data.registrationId;
                                $rootScope.user.ReceiveNotifications = true;
                                $rootScope.saveUserChanges();
                            }).catch($rootScope.handleError);
                        });
                        pN.on('error', function (e) {
                            console.error(e.message);
                        });

                        pN.on('notification', function (data) {
                            console.log(data);

                            $rootScope.notificationMsg = data.message;
                            if (!$scope.$digest) $scope.$apply();

                            $('#pushModal').modal();
                        });
                    }
                });
            };

            init();
        });
    }]);

    app.config(['$routeProvider', '$locationProvider', '$httpProvider', function ($routeProvider, $locationProvider, $httpProvider) {

        $routeProvider
            .when('/home',
            {
                templateUrl: 'scripts/app/home.html',
                controller: 'homeController'
            })
        .when('/roster',
        {
            templateUrl: 'scripts/app/roster.html',
            controller: 'rosterController'
        })
              .when('/leaderboard',
        {
            templateUrl: 'scripts/app/leaderboard.html',
            controller: 'leaderboardController'
        })
        .when('/settings/',
        {
            templateUrl: 'scripts/app/settings.html',
            controller: 'settingsController'
        })
            .when('/friends',
        {
            templateUrl: 'scripts/app/friends.html',
            controller: 'friendsController'
        })
            .when('/stats',
        {
            templateUrl: 'scripts/app/stats.html',
            controller: 'statsController'
        });


        $locationProvider.html5Mode(false);

    }]);

    //master controller for nav and global elements/content
    app.controller('masterController', ['$scope', '$rootScope', '$cordovaOauth', '$location', '$http', function ($scope, $rootScope, $cordovaOauth, $location, $http) {
        document.addEventListener("deviceready", function () {

            $rootScope.handleError = function (error) {
                if (typeof error === 'string')
                    console.error(error);
                else
                    console.error(JSON.stringify(error));
            };

            $rootScope.showError = function (error) {

                if (typeof error === typeof '')
                    $rootScope.errorMessage = error;

                if (typeof error === typeof {}) {

                    if (typeof error.data == typeof {} && error.data.Message) {

                        $rootScope.errorMessage = error.data.Message;
                        $('#errorModal').modal();
                        return;
                    }

                    if (error.status) {
                        $rootScope.errorMessage = error.status + ': ' + error.statusText;
                    }
                    else {
                        $rootScope.errorMessage = JSON.stringify(error);
                    }
                }

                $('#errorModal').modal();
            };

     

            //toggles.. the menu!
            $rootScope.menuOpen = false;
            $rootScope.toggleMenu = function () {
                $rootScope.menuOpen = !$rootScope.menuOpen;
            };

            $('body').on('click', 'div.menu a.list-group-item', function () {
                $rootScope.menuOpen = false;
                if (!$scope.$$phase) $scope.$digest();
            });

            //watches route changes.
            $rootScope.$on('$routeChangeStart', function (e, next, current) {
                $('.modal').modal('hide'); //house keeping.             

                if (next.$$route.originalPath !== '/home' && !$rootScope.user.loggedIn) {
                    $location.path('/home');
                }
            });

            $rootScope.$on('$routeChangeSuccess', function (e, current, previous) {
                $rootScope.path = $location.path().replace(/\//ig, '');
            });

            $rootScope.logout = function () {
                $rootScope.loggingIn = false;
                //TODO: unregister push
                $http.defaults.headers.common.Authorization = '';

                localStorage.clear();
                $rootScope.user = defaultUser;
                $location.path('/home');
            };



            //uses login data to authorize with fantasy dead api
            var authorize = function (token) {

                $http.defaults.headers.common.Authorization = 'Bearer ' + token;
                $http.get($rootScope.fdApi + 'api/person').then(function (response) {

                    var u = response.data;
                    $rootScope.user = u;
                    $rootScope.user.Token = token;
                    $rootScope.user.loggedIn = true;
                    $rootScope.user.isNewUser = u.Role === 1;

                    $rootScope.deadlineLockHours = null;
                    if ($rootScope.user.Configuration.DeadlineLockHours > 0)
                        $rootScope.deadlineLockHours = { hours: $rootScope.user.Configuration.DeadlineLockHours, d: $rootScope.user.Configuration.DeadlineLockHours + ' hour(s) before' };
                    $rootScope.saveUserChanges();

                    if ($rootScope.user.isNewUser) {
                        $location.path('/settings');
                        $rootScope.user.askNotify = true;
                    }
                    else
                        $location.path('/roster');

                    if (!$rootScope.$$phase)
                        $rootScope.$digest();

                }).catch(function (error) {
                    $rootScope.loggingIn = false;
                    $rootScope.handleError(error);
                    $rootScope.showError('There was an error while logging you in. Please try again later.');
                });


            };

            //// LOGINS

            //starts a facebook login flow
            $rootScope.facebookLogin = function () {

                $('#loginModal').modal('hide');
                $rootScope.loggingIn = true;
                $cordovaOauth.facebook('511139795716212', ['email', 'public_profile'], loginOptions).then(function (result) {

                    var lr = { token: result.access_token, platform: 1 };
                    $http.put($rootScope.fdApi + 'api/register/social', lr).then(function (response) {
                        authorize(response.data);
                    }).catch(function (error) {
                        $rootScope.handleError(error);
                        $rootScope.showError('Could not log you into Facebook: Login servers had an issue. Please try again later.');
                    });
                })
            };

            //start a google login flow
            $rootScope.googleLogin = function () {
                $('#loginModal').modal('hide');
                $rootScope.loggingIn = true;
                $cordovaOauth.google('231469866429-ci03k4pvepithnhft6ols5oq896um4fi.apps.googleusercontent.com', ['email', 'profile'],
                    loginOptions).then(function (result) {


                        var lr = { token: result.access_token, platform: 2 };
                        $http.put($rootScope.fdApi + 'api/register/social', lr).then(function (response) {
                            authorize(response.data);
                        }).catch(function (error) {
                            $rootScope.handleError(error);
                            $rootScope.showError('Could not log you into Google: Login servers had an issue. Please try again later.');
                        });

                        //$http.get('https://www.googleapis.com/oauth2/v1/userinfo?alt=json&access_token=' + result.access_token)
                        //    .then(function (response) {
                        //        authorize('google', response.data);
                        //    }).catch(function (error) {
                        //        console.error(error);
                        //    });
                    }, function (error) {
                        console.error(error);
                    });
            };

            //starts a twitter login flow
            $rootScope.twitterLogin = function () {
                $('#loginModal').modal('hide');
                $rootScope.loggingIn = true;
                $cordovaOauth.twitter('EBJIutCaB6XiNvbGe6oexBYKf', '16XiJIz8j9KKFFNYeAkt5EZEMQqE5Rtc3tPBbFadnxR8VaGWzR', loginOptions).then(function (data) {
                    try {

                        var lr = { token: data.oauth_token + ',' + data.oauth_token_secret, platform: 0 };
                        $http.put($rootScope.fdApi + 'api/register/social', lr).then(function (response) {
                            authorize(response.data);
                        }).catch(function (error) {
                            $rootScope.handleError(error);
                            $rootScope.loggingIn = false;
                            $rootScope.showError('Could not log you into Twitter: Login servers had an issue. Please try again later.');
                        });

                    } catch (e) {
                        console.error(JSON.stringify(e));
                    }

                }, function (error) {
                    console.error(error);
                });
            };

            //starts a microsoft login flow
            $rootScope.microsoftLogin = function () {
                $('#loginModal').modal('hide');
                $rootScope.loggingIn = true;
                $cordovaOauth.windowsLive('0000000040167932', ['wl.basic', 'wl.emails'], loginOptions).then(function (result) {


                    var lr = { token: result.access_token, platform: 3 };
                    $http.put($rootScope.fdApi + 'api/register/social', lr).then(function (response) {
                        authorize(response.data);
                    }).catch(function (error) {
                        $rootScope.handleError(error);
                        $rootScope.showError('Could not log you into Microsoft: Login servers had an issue. Please try again later.');
                    });
                }, function (error) {
                    console.error(error);
                });
            };

            //updates the user in storage and in the api.
            $rootScope.saveUserChanges = function (updateApi) {
                if (!$rootScope.user) {
                    console.error('user invalid, cannot save.');
                    return;
                }

                //update api
                if (updateApi === undefined)
                    updateApi = false;
                if (updateApi) {
                    //api returns truer object, inject that
                    //$rootScope.user = ...
                }

                $rootScope.user.ReceiveNotifications = ($rootScope.user.PushRegistration !== null);

                localStorage.setItem('user', JSON.stringify($rootScope.user));
            };




            //toggle the configuration for notifications on score completion
            $rootScope.toggleNotifyWhenScored = function () {
                $rootScope.user.Configuration.NotifyWhenScored = !$rootScope.user.Configuration.NotifyWhenScored;
                $rootScope.updateConfiguration('NotifyWhenScored', $rootScope.user.Configuration.NotifyWhenScored);
            };

            //toggling notifications all up
            $rootScope.toggleNotifications = function (force) {

                $rootScope.user.Configuration.ReceiveNotifications = !$rootScope.user.Configuration.ReceiveNotifications;

                if (typeof force === typeof true)
                    $rootScope.user.Configuration.ReceiveNotifications = force;

                if ($rootScope.user.Configuration.ReceiveNotifications) {
                    $rootScope.setupPushNotification();
                }
                else {
                    $rootScope.destroyPushSetup();
                }

            };

            //fires configuration change to api
            $rootScope.updateConfiguration = function (key, value) {
                $http.post($rootScope.fdApi + 'api/person/config/' + key, value).then(function (response) {
                    $rootScope.saveUserChanges();
                }).catch(function (error) {
                    $rootScope.handleError(error);
                    $rootScope.showError(error);
                });
            };

            //minor object setup
            $rootScope.preLockHours = [
                { hours: 1, d: '1 Hour Before' },
                { hours: 2, d: '2 Hours Before' },
                { hours: 4, d: '4 Hours Before' },
                { hours: 6, d: '6 Hours Before' },
                { hours: 12, d: '12 Hours Before' },
                { hours: 24, d: '24 Hours Before' },
                { hours: 48, d: '48 Hours Before' },
                { hours: 72, d: '72 Hours Before' }
            ];
        });
    }]);
})();

