(function () {

    var app = angular.module('admin', ['ngRoute', 'ui.bootstrap.datetimepicker']);

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


    app.config(['$routeProvider', function ($routeProvider) {

        $routeProvider
            .when('/landing',
            {
                templateUrl: '/app/landing.html',
                controller: 'landingController'
            })
            .when('/login',
            {
                templateUrl: '/app/login.html',
                controller: 'loginController'
            })
              .when('/characters',
            {
                templateUrl: '/app/characters.html',
                controller: 'charactersController'
            })
              .when('/events',
            {
                templateUrl: '/app/events.html',
                controller: 'eventsController'
            })
              .when('/score',
            {
                templateUrl: '/app/score.html',
                controller: 'scoreController'
            })
        .otherwise('/landing');



    }]);


    app.run(['$rootScope', '$location', '$http', function ($rootScope, $location, $http) {


        $rootScope.user = null;
        var authObj = getCookie('fdAuth');

        if (authObj !== '') {
            try {
                var user = JSON.parse(atob(authObj));
                $rootScope.user = user;
                $http.defaults.headers.common.Authorization = 'Bearer ' + getCookie('fdToken');
                $rootScope.loggedIn = true;
            } catch (e) {
                //swallow & clear 
                $rootScope.signOut(false);
                console.warn('Could not log user in from cookie.');
            }
        }

        $rootScope.$on('$locationChangeStart', function (ev, newUrl) {
            if ($rootScope.user === null && newUrl.indexOf('login') == -1) {
                $rootScope.loggedIn = false;
                $location.path('/login');
            }
        });

        $rootScope.errorMsg = null;

        //handles any text error.
        $rootScope.handleError = function (msg) {
            $rootScope.errorMsg = msg;
        };

        //handles any response error.
        $rootScope.handleHttpError = function (httpResponse) {
            if (httpResponse.data === '' || (typeof httpResponse.data === 'string' && httpResponse.data.indexOf('<') !== -1)) {
                httpResponse.data = { Message: httpResponse.statusText };
            }
            $rootScope.errorMsg = httpResponse.status + ': ' + httpResponse.data.Message;
        }

        //closes error dialog box
        $rootScope.closeError = function () {
            $rootScope.errorMsg = null;
        };

        //signs the user out
        $rootScope.signOut = function (redirect) {
            if (redirect === undefined)
                redirect = true;
            $rootScope.loggedIn = false;
            $rootScope.user = null;
            setCookie('fdAuth', '', -1);
            setCookie('fdToken', '', -1);
            $location.path('/login');
        };

        $rootScope.shows = [];
        $rootScope.fetchShowData = function (contWith) {
            if ($rootScope.loggedIn) {
                $http.get('api/configuration/shows').then(function (response) {
                    $rootScope.shows = response.data;
                }).catch($rootScope.handleHttpError);
            }

            if (typeof (contWith) === 'function')
                contWith();
        };
        $rootScope.fetchShowData();


        $rootScope.uploadImage = function (file, folder, contWith) {
            var uploadUrl = 'api/configuration/image/' + folder;
            var fd = new FormData();
            fd.append('file', file);
            $http.post(uploadUrl, fd, {
                transformRequest: angular.identity,
                headers: { 'Content-Type': undefined }
            }).then(function (response) {

                //TODO START HERE
                if (typeof contWith === 'function')
                    contWith(response.data);
            });
        };

    }]);

    app.controller('charactersController', ['$scope', '$rootScope', '$http', function ($scope, $rootScope, $http) {

        //initialize
        $scope.init = function () {

            if ($rootScope.shows.length > 0) {
                $scope.selectedShow = $rootScope.shows[0];
                $scope.getCharacterList();
            }
        };

        //fetches the character list
        $scope.loading = false;
        $scope.getCharacterList = function () {
            $scope.loading = true;
            $http.get('api/configuration/characters/' + $scope.selectedShow.id).then(function (response) {
                $scope.characters = response.data;
                $scope.loading = false;
            }).catch($rootScope.handleHttpError);
        };

        //launch edit/add modal
        $scope.editCharacter = function (ch) {

            if (!ch)
                ch = { ShowId: $scope.selectedShow.id };

            $scope.eCh = ch;
            $('#editCh').modal();
        };

        $scope.uploading = false;
        $scope.uploadFile = function () {
            $scope.uploading = true;
            var file = $scope.newImg;
            //send here
            $rootScope.uploadImage(file, 'chars', function (url) {
                $scope.eCh.PrimaryImageUrl = url;
                $scope.showUpload = false;
                $scope.uploading = false;
            });

        };

        //save changes
        $scope.saving = false;
        $scope.saveCharacter = function () {
            $scope.saving = true;
            $http.put('api/character', $scope.eCh).then(function (response) {

                $scope.getCharacterList($scope.eCh.ShowId);
                $scope.saving = false;
                $('#editCh').modal('hide');
            }).catch(function (error) {
                $rootScope.handleHttpError(error);
                $scope.saving = false;
                $('#editCh').modal('hide');
            });
        };

        $scope.init();
    }]);



    app.controller('eventsController', ['$scope', '$rootScope', '$http', function ($scope, $rootScope, $http) {

        $scope.init = function () {

            if ($rootScope.shows.length > 0) {
                $scope.selectedShow = $rootScope.shows[0];
                $scope.fetchConfigurations();
            }

            $scope.categories = [
                { id: 0, Name: 'Trivial' },
                { id: 1, Name: 'Supportive' },
                { id: 2, Name: 'Walker Kill' },
                { id: 3, Name: 'Moral' },
                { id: 4, Name: 'Human Kill' },
                { id: 5, Name: 'Detrimental' },
                { id: 6, Name: 'Death' }
            ];

            $scope.modTypes = [
                { id: 0, Name: 'Add' },
                { id: 1, Name: 'Subtract' },
                { id: 2, Name: 'Percentage' }
            ];


        };

        $scope.categoryConvert = function (num) {
            return $scope.categories[num].Name;
        };

        $scope.typeConvert = function (num) {
            return $scope.modTypes[num].Name;
        };

        $scope.modValueConvert = function (type, v) {
            switch (type) {
                case 0: {
                    var v2 = Math.abs(v);
                    return '+' + v2 + ' points';
                }
                case 1: {
                    var v2 = Math.abs(v);
                    return '-' + v2 + ' points';
                }
                case 2: {

                    var v2 = Math.round((v - 1) * 100);
                    var pre = v > 0 ? '+' : '';
                    return pre + v2 + '%' + ' points';
                }
                default: {
                    return 'invalid';
                }
            }
        };

        $scope.convertToModTypeDisplay = function (v, mod) {
            if (mod.ModificationTypeInt === undefined) {
                $scope.modValueDisplay = 'Please select a type';
                return;
            }

            if (v === undefined || v === null || v === '') {
                $scope.modValueDisplay = '';
                return;
            }



            switch (mod.ModificationTypeInt.id) {
                case 0: {
                    v = Math.abs(v);
                    mod.ModificationValue = v;
                    $scope.modValueDisplay = '+' + v + ' points';
                    return;
                }
                case 1: {
                    mod.ModificationValue = v;
                    v = Math.abs(v);
                    $scope.modValueDisplay = '-' + v + ' points';
                    return;
                }
                case 2: {
                    if (v > 0) {
                        mod.ModificationValue = (v / 100) + 1;
                    } else {
                        mod.ModificationValue = 1 - (v / 100);
                    }

                    var pre = v > 0 ? '+' : '';

                    $scope.modValueDisplay = pre + v + '%' + ' points';
                    return;
                }
                default: {
                    $scope.modValueDisplay = 'invalid';
                    return;
                }
            }
        };

        $scope.loaded = false;
        $scope.loading = false;
        $scope.fetchConfigurations = function () {
            $scope.loading = true;
            $scope.definitions = null;
            $scope.modifiers = null;

            $http.get('api/configuration/definitions/' + $scope.selectedShow.id).then(function (response) {
                $scope.definitions = response.data;
                if ($scope.modifiers !== null) {
                    $scope.loaded = true;
                    $scope.loading = false;
                }
            }).catch($rootScope.handleHttpError);

            $http.get('api/configuration/modifiers/' + $scope.selectedShow.id).then(function (response) {
                $scope.modifiers = response.data;
                if ($scope.definitions !== null) {
                    $scope.loaded = true;
                    $scope.loading = false;
                }
            }).catch($rootScope.handleHttpError);
        };


        $scope.editDefinition = function (def) {

            if (!def)
                def = {
                    ShowId: $scope.selectedShow.id
                };

            $scope.eDef = def;
            $('#editDef').modal();
        };

        $scope.editModifier = function (mod) {

            if (!mod)
                mod = {
                    ShowId: $scope.selectedShow.id
                };

            $scope.eMod = mod;
            $('#editMod').modal();
        };


        $scope.saving = false;
        $scope.saveDefinition = function () {
            $scope.saving = true;
            $scope.eDef.CategoryInt = $scope.eDef.CategoryInt.id;

            $http.put('api/configuration/definition', $scope.eDef).then(function (response) {

                $scope.init();
                $scope.saving = false;
                $('#editDef').modal('hide');
            }).catch(function (error) {
                $rootScope.handleHttpError(error);
                $scope.saving = false;
                $('#editDef').modal('hide');
            });
        };

        $scope.saveModifier = function () {
            $scope.saving = true;
            $scope.eMod.ModificationTypeInt = $scope.eMod.ModificationTypeInt.id;

            $http.put('api/configuration/modifier', $scope.eMod).then(function (response) {

                $scope.init();
                $scope.saving = false;
                $('#editMod').modal('hide');
            }).catch(function (error) {
                $rootScope.handleHttpError(error);
                $scope.saving = false;
                $('#editMod').modal('hide');
            });
        };

        $scope.init();
    }]);


    app.controller('landingController', ['$scope', '$rootScope', '$http', function ($scope, $rootScope, $http) {
        $http.defaults.headers.common.Authorization = 'Bearer ' + $rootScope.user.token;

        //add/edit the episode
        $scope.editEpisode = function (ep, showId, seasonId) {

            if (!ep)
                ep = {
                    ShowId: showId,
                    SeasonId: seasonId
                };

            $scope.eEp = ep;
            $('#editEp').modal();
        };

        $scope.saving = false;
        //save the episode
        $scope.saveEpisode = function () {

            $scope.saving = true;
            $http.put('api/show/season/episode', $scope.eEp).then(function (response) {
                $('#editEp').modal('hide');
                $rootScope.fetchShowData();
            }).catch(function (error) {
                $scope.saving = false;
                $('#editEp').modal('hide');
                $rootScope.handleHttpError(error);
            });
        };

        $scope.ready = true;
    }]);


    app.controller('scoreController', ['$scope', '$rootScope', '$http', '$timeout', function ($scope, $rootScope, $http, $timeout) {

        $scope.init = function () {
            if ($rootScope.shows.length > 0) {
                $scope.selectedShow = $rootScope.shows[0];
                $scope.selectSeason = true;
                $scope.fetchConfigurations();
            }
            $scope.clear();
        };

        $scope.ready = false;
        $scope.loading = false;
        $scope.fetchEvents = function () {
            $scope.events = [];
            $scope.calculating = false;
            $scope.calcPercent = 0;
            $scope.loading = true;
            $http.get('api/statistics/events/episode/' + $scope.selectedEp.id).then(function (response) {
                $scope.events = response.data;
                $scope.ready = true;
                $scope.loading = false;
            }).catch(function (error) {
                $scope.ready = true;
                $scope.loading = false;
                $rootScope.handleHttpError(error);
            });
        };


        $scope.fetchConfigurations = function () {
            $scope.definitions = null;
            $scope.modifiers = null;

            $http.get('api/configuration/definitions/' + $scope.selectedShow.id).then(function (response) {
                $scope.definitions = response.data;
            }).catch($rootScope.handleHttpError);

            $http.get('api/configuration/modifiers/' + $scope.selectedShow.id).then(function (response) {
                $scope.modifiers = response.data;
            }).catch($rootScope.handleHttpError);

            $http.get('api/configuration/characters/' + $scope.selectedShow.id).then(function (response) {
                $scope.characters = response.data;
            }).catch($rootScope.handleHttpError);
        };


        $scope.populateSeasons = function () {
            $scope.fetchConfigurations();
            $scope.selectSeason = true;
        };

        $scope.selectChar = function (ch) {
            if ($scope.selectedChar && ch.Id === $scope.selectedChar.Id) {
                $scope.selectedChar = null;
                $scope.validate();
                return;
            }
            $scope.selectedChar = ch;
            $scope.validate();
        };

        $scope.selectDef = function (def) {
            $('#timestamp-minutes').focus();
            if ($scope.selectedDef && def.Id === $scope.selectedDef.Id) {
                $scope.selectedDef = null;
                $scope.validate();
                return;
            }
            $scope.selectedDef = def;
            $scope.validate();
        };

        $scope.selectMod = function (mod) {
            $('#timestamp-minutes').focus();
            if ($scope.selectedMod && mod.Id === $scope.selectedMod.Id) {
                $scope.selectedMod = null;
                $scope.validate();
                return;
            }
            $scope.selectedMod = mod;
            $scope.validate();
        };

        $scope.clear = function () {
            $scope.selectedMod = null;
            $scope.selectedDef = null;
            $scope.selectedChar = null;
            $scope.ev = {};
            $scope.validate();
            $scope.epTimestamp = {};
        };

        $scope.formTimeStamp = function () {
            var ogM = $scope.epTimestamp.m;
            var ogS = $scope.epTimestamp.s;

            var mins = parseInt(ogM);
            var seconds = parseInt(ogS);
            if (seconds > 60)
                seconds = 59;

            $scope.epTimestamp.s = seconds;

            var minsInSeconds = mins * 60;
            var ts = minsInSeconds + seconds;
            $scope.ev.EpisodeTimestamp = ts;
            $scope.validate();
            return ts;
        };

        $scope.parseTimestamp = function (totalSeconds) {

            if (totalSeconds < 60)
                return '00:' + totalSeconds;

            var mins = Math.floor(totalSeconds / 60);
            var secs = totalSeconds % 60;

            var minsStr = mins > 9 ? '' + mins : '0' + mins;
            var secStr = secs > 9 ? '' + secs : '0' + secs;
            return minsStr + ':' + secStr;
        };

        $scope.valid = false;
        $scope.validate = function () {

            $scope.valid = false;
            if (!$scope.selectedChar) {
                $scope.validationMsg = 'Character not selected.';
                return;
            }
            if (!$scope.selectedDef) {
                $scope.validationMsg = 'Definition not selected.';
                return;
            }
            if (!$scope.ev.EpisodeTimestamp || !$scope.ev.EpisodeTimestamp === 0) {
                $scope.validationMsg = 'Timestamp is not valid yet.';
                return;
            }

            $scope.validationMsg = 'Event is valid.';
            $scope.valid = true;
        };


        $scope.adding = false;
        $scope.addEvent = function () {
            $scope.adding = true;

            var ev = $scope.ev;

            ev.ShowId = $scope.selectedShow.id;
            ev.EpisodeId = $scope.selectedEp.id;
            ev.ActionId = $scope.selectedDef.Id;
            if ($scope.selectedMod)
                ev.ModifierId = $scope.selectedMod.Id;

            ev.DeathEvent = ($scope.selectedDef.CategoryInt === 6)

            ev.PartitionKey = $scope.selectedChar.Id;

            $http.put('api/event', ev).then(function (response) {
                $scope.adding = false;
                $scope.events.push(ev);
                $scope.ev = {};
                $scope.formTimeStamp();
                $scope.fetchEvents();
            }).catch($rootScope.handleHttpError);

        };


        $scope.deleteEv = function (ev) {
            ev.deleting = true;
            //api/event/{eventId}/character/{characterId}
            $http.delete('api/event/' + ev.Id + '/character/' + ev.PartitionKey).then(function (response) {
                $scope.events = $.grep($scope.events, function (e) {
                    return e.Id !== ev.Id;
                });

            }).catch($rootScope.handleHttpError);
        };

        $scope.calculating = false;
        $scope.calcCheckLoop = function (calcId) {

            if ($scope.calcPercent >= 100)
                return;

            $http.get('api/event/progress/' + calcId).then(function (response) {
                $scope.calcPercent = response.data;
                $timeout(function () {

                    $scope.calcCheckLoop(calcId);
                }, 3000);
            }).catch($rootScope.handleHttpError);
        };



        $scope.calculate = function () {
            $scope.calculating = true;
            $scope.calcPercent = 0;
            $http.get('api/event/calculate/' + $scope.selectedEp.id).then(function (response) {
                $scope.calcCheckLoop(response.data);
            }).catch($rootScope.handleHttpError);
        };

        $scope.init();
    }]);

    ///////////// LOGIN
    app.controller('loginController', ['$scope', '$rootScope', '$http', '$location', function ($scope, $rootScope, $http, $location) {

        $scope.id = {};
        $scope.working = false;
        $scope.signIn = function (id) {
            $scope.working = true;
            if (id.Credentials === '' || id.PlatformUserId === '') {
                $rootScope.handleError('Email / Password cannot be blank.');
                $scope.working = false;
                return;
            }

            id.Credentials = btoa(id.Credentials);
            id.PlatformName = 'Custom';

            $http.put('/api/register/login', id).then(function (response) {

                var token = response.data;
                $http.defaults.headers.common.Authorization = 'Bearer ' + token;

                $http.get('api/person').then(function (pResponse) {

                    $rootScope.user = pResponse.data;
                    $rootScope.user.token = token;
                    var uJsonData = btoa(JSON.stringify(pResponse.data));
                    setCookie('fdAuth', uJsonData, 7);
                    setCookie('fdToken', token, 7);
                    $rootScope.loggedIn = true;
                    $location.path('/landing');

                    $rootScope.fetchShowData();

                });

            }).catch(function (response) {
                $rootScope.handleHttpError(response);
                $scope.id.Credentials = '';
                $scope.working = false;
            });

        };

    }]);

    var setCookie = function (cname, cvalue, exdays) {
        var d = new Date();
        d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
        var expires = "expires=" + d.toUTCString();
        document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/";
    };

    var getCookie = function (cname) {
        var name = cname + "=";
        var ca = document.cookie.split(';');
        for (var i = 0; i < ca.length; i++) {
            var c = ca[i];
            while (c.charAt(0) == ' ') {
                c = c.substring(1);
            }
            if (c.indexOf(name) == 0) {
                return c.substring(name.length, c.length);
            }
        }
        return "";
    };

})();